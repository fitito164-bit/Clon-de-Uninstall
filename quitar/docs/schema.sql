-- Esquema local (SQLite) del clon de Uninstall Tool. Fase 1: nucleo.
-- No hay usuarios ni servidor: la base vive en %LOCALAPPDATA% del usuario de Windows.
-- Los programas instalados NO se guardan: se leen del registro en cada arranque (el registro es la fuente de verdad).
-- Tiempos: texto ISO 8601 en UTC (equivalente a timestamptz).

pragma foreign_keys = on;
pragma journal_mode = wal;

-- Ajustes (S14). Una fila por clave; el valor es texto o JSON.
create table settings (
  key        text primary key,
  value      text not null,
  updated_at text not null default (strftime('%Y-%m-%dT%H:%M:%fZ','now'))
);

-- Una ejecucion de limpieza (flujos F01 a F04).
create table removal_runs (
  id              text primary key,                 -- uuid generado por la app
  program_key     text not null,                    -- subclave del registro o nombre completo del paquete de Store
  program_name    text not null,
  program_version text,
  source          text not null
    check (source in ('registry_hklm','registry_hklm_wow64','registry_hkcu','store')),
  mode            text not null
    check (mode in ('uninstall','force','batch_force','remove_entry')),
  batch_id        text,                             -- agrupa los programas de un mismo lote (F03)
  status          text not null default 'scanning'
    check (status in ('scanning','reviewing','deleting','done','cancelled','failed')),
  reboot_pending  integer not null default 0 check (reboot_pending in (0,1)),
  started_at      text not null default (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
  ended_at        text,
  constraint run_time_order check (ended_at is null or ended_at >= started_at)
);
create index idx_runs_program on removal_runs (program_key, started_at);
create index idx_runs_batch   on removal_runs (batch_id);
create index idx_runs_started on removal_runs (started_at);

-- Restos encontrados por el escaneo (S07, S12).
-- Si se borra la ejecucion, se borran sus restos y su registro: el historial se limpia junto.
create table leftover_items (
  id         text primary key,
  run_id     text not null references removal_runs(id) on delete cascade,
  kind       text not null
    check (kind in ('file','folder','reg_key','reg_value','service','driver','com_object',
                    'file_assoc','installer_component','shortcut')),
  path       text not null,
  value_name text not null default '',             -- solo para reg_value; vacio en el resto (asi la unicidad funciona)
  selected   integer not null default 1 check (selected in (0,1)),
  risk       text not null default 'low' check (risk in ('low','medium','high')),
  reason     text not null,                        -- por que se considero resto (se muestra al usuario)
  in_use     integer not null default 0 check (in_use in (0,1)),
  state      text not null default 'found'
    check (state in ('found','deleted','failed','deferred','skipped')),
  unique (run_id, kind, path, value_name)
);
create index idx_leftovers_run   on leftover_items (run_id, state);
create index idx_leftovers_risk  on leftover_items (run_id, risk);

-- Registro de operaciones (S10). Solo se agrega, nunca se edita.
create table removal_log_entries (
  id         integer primary key autoincrement,
  run_id     text not null references removal_runs(id) on delete cascade,
  ts         text not null default (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
  operation  text not null
    check (operation in ('terminate_process','run_uninstaller','delete_file','delete_folder',
                         'delete_reg_key','delete_reg_value','stop_service','delete_service',
                         'unregister_com','schedule_delete','backup_registry','scan')),
  target     text not null,
  result     text not null check (result in ('ok','failed','skipped','deferred')),
  error_code integer,                              -- codigo de Windows, si hubo
  message    text
);
create index idx_log_run on removal_log_entries (run_id, ts);

-- Borrados programados para el proximo reinicio (S09).
-- Si se borra la ejecucion, el pendiente se conserva (set null): el archivo sigue ahi hasta el reinicio.
create table pending_deletes (
  id          integer primary key autoincrement,
  run_id      text references removal_runs(id) on delete set null,
  path        text not null unique,
  kind        text not null check (kind in ('file','folder')),
  scheduled_at text not null default (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
  resolved_at text
);
create index idx_pending_open on pending_deletes (resolved_at);

-- Respaldo de claves del registro exportadas antes de borrar (mejora propia, regla "nunca borrar sin respaldo").
create table registry_backups (
  id            integer primary key autoincrement,
  run_id        text not null references removal_runs(id) on delete cascade,
  key_path      text not null,
  reg_file_path text not null,                     -- archivo .reg exportado en %LOCALAPPDATA%
  created_at    text not null default (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
  restored_at   text
);
create index idx_backups_run on registry_backups (run_id);

-- Licencia (S17). Una sola fila. La firma se verifica sin conexion con una clave publica incluida en la app.
create table license (
  id               integer primary key check (id = 1),
  kind             text not null default 'trial' check (kind in ('trial','standard','portable')),
  license_key      text,
  email            text,
  signature        text,
  trial_started_at text not null default (strftime('%Y-%m-%dT%H:%M:%fZ','now')),
  activated_at     text
);

-- ---------------------------------------------------------------------------
-- FASE 2 (no aplicar todavia): gestor de inicio y rastreo de instalaciones.
-- El gestor de inicio no necesita tablas: lee registro, carpetas de Inicio y tareas programadas en vivo.
--
-- create table trace_sessions (
--   id text primary key, program_key text, main_item integer default 1,
--   parent_id text references trace_sessions(id) on delete cascade,
--   started_at text not null, ended_at text, reverted_at text);
-- create table trace_entries (
--   id integer primary key autoincrement,
--   session_id text not null references trace_sessions(id) on delete cascade,
--   kind text not null check (kind in ('file','folder','reg_key','reg_value')),
--   path text not null, value_name text not null default '',
--   unique (session_id, kind, path, value_name));
-- create index idx_trace_session on trace_entries (session_id);
