-- =============================================================================
-- ESQUEMA DDL COMPLETO EN POSTGRESQL PARA "ASISTENTE DE EVENTOS DE NEGOCIO"
-- Conforme al estándar IEEE 830 y modelo de datos Multi-Tenant con aislamiento estricto
-- =============================================================================

-- 1. Tabla: Pymes
CREATE TABLE IF NOT EXISTS pymes (
    id SERIAL PRIMARY KEY,
    razon_social VARCHAR(200) NOT NULL,
    nit VARCHAR(30) NOT NULL UNIQUE,
    pais VARCHAR(100) NOT NULL DEFAULT 'Colombia',
    departamento VARCHAR(100),
    municipio VARCHAR(100),
    ciudad VARCHAR(100),
    sector VARCHAR(100),
    fecha_registro TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 2. Tabla: Usuarios
CREATE TABLE IF NOT EXISTS usuarios (
    id SERIAL PRIMARY KEY,
    pyme_id INT NOT NULL REFERENCES pymes(id) ON DELETE CASCADE,
    nombre VARCHAR(150) NOT NULL,
    correo VARCHAR(150) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    rol VARCHAR(50) NOT NULL DEFAULT 'Administrador',
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_registro TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_usuarios_pyme_id ON usuarios(pyme_id);

-- 3. Tabla: Eventos
CREATE TABLE IF NOT EXISTS eventos (
    id SERIAL PRIMARY KEY,
    pyme_id INT NOT NULL REFERENCES pymes(id) ON DELETE CASCADE,
    nombre VARCHAR(200) NOT NULL,
    descripcion TEXT,
    fecha_evento TIMESTAMP WITH TIME ZONE NOT NULL,
    ubicacion VARCHAR(200),
    aforo_estimado INT NOT NULL DEFAULT 0,
    presupuesto_base NUMERIC(18, 2) NOT NULL DEFAULT 0.00,
    estado VARCHAR(50) NOT NULL DEFAULT 'En Planificación', -- 'En Planificación', 'En Proceso', 'Completado'
    fecha_creacion TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_eventos_pyme_id ON eventos(pyme_id);

-- 4. Tabla: Tareas (RF06)
CREATE TABLE IF NOT EXISTS tareas (
    id SERIAL PRIMARY KEY,
    pyme_id INT NOT NULL REFERENCES pymes(id) ON DELETE CASCADE,
    evento_id INT NOT NULL REFERENCES eventos(id) ON DELETE CASCADE,
    nombre VARCHAR(200) NOT NULL,
    descripcion TEXT,
    responsable VARCHAR(150),
    fecha_vencimiento TIMESTAMP WITH TIME ZONE NOT NULL,
    estado VARCHAR(50) NOT NULL DEFAULT 'Pendiente', -- 'Pendiente', 'En Proceso', 'Completada'
    fecha_creacion TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_tareas_pyme_id ON tareas(pyme_id);
CREATE INDEX IF NOT EXISTS idx_tareas_evento_id ON tareas(evento_id);

-- 5. Tabla: Rubros Presupuestales (RF05)
CREATE TABLE IF NOT EXISTS rubros_presupuestales (
    id SERIAL PRIMARY KEY,
    pyme_id INT NOT NULL REFERENCES pymes(id) ON DELETE CASCADE,
    evento_id INT NOT NULL REFERENCES eventos(id) ON DELETE CASCADE,
    categoria VARCHAR(100) NOT NULL, -- 'Logística', 'Insumos/Comida', 'Publicidad', 'Imprevistos'
    porcentaje NUMERIC(5, 2) NOT NULL DEFAULT 0.00,
    valor_estimado NUMERIC(18, 2) NOT NULL DEFAULT 0.00
);
CREATE INDEX IF NOT EXISTS idx_rubros_pyme_id ON rubros_presupuestales(pyme_id);
CREATE INDEX IF NOT EXISTS idx_rubros_evento_id ON rubros_presupuestales(evento_id);

-- 6. Tabla: Gastos Reales (RF05)
CREATE TABLE IF NOT EXISTS gastos (
    id SERIAL PRIMARY KEY,
    pyme_id INT NOT NULL REFERENCES pymes(id) ON DELETE CASCADE,
    evento_id INT NOT NULL REFERENCES eventos(id) ON DELETE CASCADE,
    descripcion VARCHAR(250) NOT NULL,
    categoria VARCHAR(100) NOT NULL,
    valor NUMERIC(18, 2) NOT NULL DEFAULT 0.00,
    fecha TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_gastos_pyme_id ON gastos(pyme_id);
CREATE INDEX IF NOT EXISTS idx_gastos_evento_id ON gastos(evento_id);

-- 7. Tabla: Notificaciones (RF07)
CREATE TABLE IF NOT EXISTS notificaciones (
    id SERIAL PRIMARY KEY,
    pyme_id INT NOT NULL REFERENCES pymes(id) ON DELETE CASCADE,
    evento_id INT REFERENCES eventos(id) ON DELETE CASCADE,
    tarea_id INT REFERENCES tareas(id) ON DELETE SET NULL,
    mensaje VARCHAR(500) NOT NULL,
    tipo VARCHAR(50) NOT NULL DEFAULT 'General',
    leida BOOLEAN NOT NULL DEFAULT FALSE,
    fecha_creacion TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_notificaciones_pyme_id ON notificaciones(pyme_id);

-- 8. Tabla: ConversacionMensaje (RF02)
CREATE TABLE IF NOT EXISTS conversacion_mensajes (
    id SERIAL PRIMARY KEY,
    pyme_id INT NOT NULL REFERENCES pymes(id) ON DELETE CASCADE,
    evento_id INT REFERENCES eventos(id) ON DELETE CASCADE,
    rol VARCHAR(20) NOT NULL, -- 'user', 'model', 'system'
    contenido TEXT NOT NULL,
    fecha_creacion TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
CREATE INDEX IF NOT EXISTS idx_mensajes_pyme_id ON conversacion_mensajes(pyme_id);
