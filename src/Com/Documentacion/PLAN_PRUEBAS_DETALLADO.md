# 📋 Plan de Pruebas Detallado - CarnetQR Platform
## Guía Paso a Paso para Personas sin Conocimiento Técnico

---

## 🎯 INFORMACIÓN INICIAL IMPORTANTE

### 🌐 URL de la Aplicación
```
http://164.68.99.83
```

### 🔑 Credenciales de Acceso Pre-configuradas

#### SuperAdmin (Administrador del Sistema)
- **Email:** `admin@qlservices.com`
- **Contraseña:** `Admin@123456`
- **Permisos:** Puede ver y gestionar TODAS las instituciones

#### InstitutionAdmin (Administrador de Institución Demo)
- **Email:** `admin@demo.com`
- **Contraseña:** `Admin@123456`
- **Permisos:** Solo puede ver y gestionar su institución "Empresa Demo"

---

## 📝 PREPARACIÓN: DATOS DE PRUEBA A CREAR CON SUPERADMIN

**IMPORTANTE:** Antes de comenzar las pruebas, el SuperAdmin debe crear estos datos de prueba:

### Institución de Prueba (Crear con SuperAdmin)
1. **Nombre:** `Hospital San José`
2. **Descripción:** `Hospital de prueba para testing`
3. **Tipo:** `Hospital`
4. **Prefijo de Tarjeta:** `HSJ`
5. **Estado:** `Activo`

### Usuarios de Prueba (Crear con SuperAdmin)
1. **Usuario Staff:**
   - Email: `staff@hospital.com`
   - Nombre: `Juan`
   - Apellido: `Pérez`
   - Rol: `Staff`
   - Institución: `Hospital San José`
   - Contraseña: `Staff@123456`

2. **Usuario AdministrativeOperator:**
   - Email: `operador@hospital.com`
   - Nombre: `María`
   - Apellido: `González`
   - Rol: `AdministrativeOperator`
   - Institución: `Hospital San José`
   - Contraseña: `Operador@123456`

### Entidades de Prueba (Crear con SuperAdmin)
1. **Entidad con Foto:**
   - Institución: `Hospital San José`
   - Número de Identificación: `8-123-4567`
   - Nombre: `Carlos`
   - Apellido: `Rodríguez`
   - Email: `carlos.rodriguez@example.com`
   - Teléfono: `507-6123-4567`
   - Fecha de Nacimiento: `15/03/1985`
   - **Foto:** Subir una imagen (JPG o PNG, máximo 2MB)

2. **Entidad sin Foto:**
   - Institución: `Hospital San José`
   - Número de Identificación: `8-234-5678`
   - Nombre: `Ana`
   - Apellido: `Martínez`
   - Email: `ana.martinez@example.com`
   - Teléfono: `507-6234-5678`
   - Fecha de Nacimiento: `20/07/1990`
   - **Foto:** NO subir foto

---

## 🔐 FASE 1: AUTENTICACIÓN Y AUTORIZACIÓN

### PRUEBA 1.1: Login con SuperAdmin - PASO A PASO

**🎯 OBJETIVO:** Verificar que puedes iniciar sesión como SuperAdmin y ver el dashboard.

**📋 PASOS DETALLADOS:**

1. **Abrir el navegador**
   - Abre Google Chrome, Microsoft Edge o Firefox
   - **IMPORTANTE:** Si ya has usado el sistema antes, abre una ventana de incógnito:
     - Chrome/Edge: Presiona `Ctrl + Shift + N`
     - Firefox: Presiona `Ctrl + Shift + P`

2. **Ir a la página de login**
   - En la barra de direcciones, escribe exactamente: `http://164.68.99.83`
   - Presiona `Enter`
   - **QUÉ DEBERÍAS VER:** La página te redirige automáticamente a `/Account/Login`

3. **Identificar los campos del formulario**
   - Deberías ver una página con:
     - Título: "CarnetQR Platform" o "Sistema de Gestión de Carnets"
     - Campo de texto: "Correo Electrónico" o "Email"
     - Campo de texto (oculto): "Contraseña" o "Password"
     - Checkbox: "Recordar sesión" (opcional)
     - Botón: "Iniciar Sesión" o "Login"

4. **Llenar el formulario**
   - En el campo "Correo Electrónico", escribe exactamente: `admin@qlservices.com`
   - En el campo "Contraseña", escribe exactamente: `Admin@123456`
   - **NOTA:** La contraseña es sensible a mayúsculas/minúsculas
   - Deja el checkbox "Recordar sesión" sin marcar (para esta prueba)

5. **Hacer click en "Iniciar Sesión"**
   - Haz click en el botón "Iniciar Sesión" o presiona `Enter`

6. **Esperar la respuesta**
   - Espera 2-3 segundos
   - **QUÉ DEBERÍAS VER:**
     - La página cambia automáticamente
     - Ya NO estás en la página de login
     - Estás en el Dashboard o página principal del sistema

**✅ RESULTADO ESPERADO:**
- ✅ Ya NO ves la página de login
- ✅ Ves un menú de navegación en la parte superior o lateral
- ✅ Ves el Dashboard con información del sistema
- ✅ En algún lugar de la pantalla ves tu nombre o email: "Super Admin" o "admin@qlservices.com"
- ✅ NO ves ningún mensaje de error en rojo

**❌ SI VES UN ERROR:**
- Si ves "HTTP ERROR 400": Limpia las cookies del navegador (ver instrucciones al final)
- Si ves "Credenciales incorrectas": Verifica que escribiste exactamente `admin@qlservices.com` y `Admin@123456`
- Si la página no carga: Verifica que escribiste correctamente la URL `http://164.68.99.83`

---

### PRUEBA 1.2: Verificar Menú de SuperAdmin

**🎯 OBJETIVO:** Verificar que el menú muestra todas las opciones disponibles para SuperAdmin.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin** (desde Prueba 1.1)

2. **Buscar el menú de navegación**
   - El menú puede estar en la parte superior (barra horizontal) o en el lado izquierdo (menú vertical)
   - Busca palabras como: "Menú", "Navegación", o iconos de hamburguesa (☰)

3. **Identificar las opciones del menú**
   - Haz click en el menú si está colapsado
   - **QUÉ DEBERÍAS VER (opciones típicas):**
     - Dashboard / Inicio
     - Entidades / Entity Profiles
     - Tarjetas / Cards
     - Eventos / Events
     - Usuarios / Users
     - Instituciones / Institutions (SOLO SuperAdmin)
     - Tipos de Institución / Institution Types (SOLO SuperAdmin)
     - Configuración / Settings
     - Cerrar Sesión / Logout

**✅ RESULTADO ESPERADO:**
- ✅ Ves al menos 6-8 opciones en el menú
- ✅ Ves la opción "Instituciones" o "Institutions" (SOLO SuperAdmin la ve)
- ✅ Ves la opción "Usuarios" o "Users"
- ✅ Todas las opciones son clickeables

---

### PRUEBA 1.3: Login con InstitutionAdmin - PASO A PASO

**🎯 OBJETIVO:** Verificar que puedes iniciar sesión como InstitutionAdmin.

**📋 PASOS DETALLADOS:**

1. **Cerrar sesión del SuperAdmin** (si estás logueado)
   - Busca la opción "Cerrar Sesión" o "Logout" en el menú
   - Haz click
   - **QUÉ DEBERÍAS VER:** Regresas a la página de login

2. **O abrir ventana de incógnito nueva**
   - Presiona `Ctrl + Shift + N` (Chrome/Edge) o `Ctrl + Shift + P` (Firefox)
   - Ve a `http://164.68.99.83`

3. **Llenar el formulario de login**
   - Email: `admin@demo.com`
   - Contraseña: `Admin@123456`
   - Click en "Iniciar Sesión"

4. **Verificar que iniciaste sesión**
   - Espera 2-3 segundos
   - **QUÉ DEBERÍAS VER:**
     - Ya NO estás en la página de login
     - Estás en el Dashboard
     - Ves el menú de navegación

**✅ RESULTADO ESPERADO:**
- ✅ Login exitoso
- ✅ Ves el Dashboard
- ✅ El menú muestra opciones (pero NO debe mostrar "Instituciones" ni "Tipos de Institución")

---

### PRUEBA 1.4: Verificar que InstitutionAdmin NO ve "Instituciones"

**🎯 OBJETIVO:** Verificar que InstitutionAdmin no puede acceder a gestión de instituciones.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como InstitutionAdmin** (desde Prueba 1.3)

2. **Revisar el menú**
   - Abre el menú de navegación
   - Busca la opción "Instituciones" o "Institutions"

**✅ RESULTADO ESPERADO:**
- ✅ NO ves la opción "Instituciones" en el menú
- ✅ NO ves la opción "Tipos de Institución" en el menú

3. **Intentar acceder directamente (opcional)**
   - En la barra de direcciones, escribe: `http://164.68.99.83/Institutions`
   - Presiona `Enter`

**✅ RESULTADO ESPERADO:**
- ✅ Ves una página de "Acceso Denegado" o "Access Denied"
- ✅ O ves un error 403
- ✅ O te redirige al Dashboard
- ✅ NO ves la lista de instituciones

---

## 👤 FASE 2: GESTIÓN DE USUARIOS

### PRUEBA 2.1: Crear Institución de Prueba (SuperAdmin)

**🎯 OBJETIVO:** Crear una institución de prueba para usar en las siguientes pruebas.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin**
   - Si no estás logueado, inicia sesión con `admin@qlservices.com` / `Admin@123456`

2. **Ir a la sección de Instituciones**
   - En el menú, busca y haz click en "Instituciones" o "Institutions"
   - **QUÉ DEBERÍAS VER:** Una página con una tabla que muestra las instituciones existentes
   - Deberías ver al menos "Empresa Demo" (creada automáticamente)

3. **Buscar el botón "Crear" o "Nuevo"**
   - Busca un botón que diga "Crear", "Nuevo", "Agregar", "Add", o "+"
   - Generalmente está en la parte superior derecha de la tabla
   - Haz click en ese botón

4. **Llenar el formulario de creación**
   - **QUÉ DEBERÍAS VER:** Un formulario con varios campos
   - Llena los campos EXACTAMENTE así:
     - **Nombre:** `Hospital San José`
     - **Descripción:** `Hospital de prueba para testing del sistema`
     - **Tipo de Institución:** Selecciona "Hospital" del dropdown
     - **Prefijo de Tarjeta:** `HSJ`
     - **Estado:** Marca el checkbox "Activo" o "IsActive" (si existe)

5. **Guardar la institución**
   - Busca el botón "Guardar", "Crear", "Save", o "Create"
   - Haz click
   - Espera 2-3 segundos

**✅ RESULTADO ESPERADO:**
- ✅ Ves un mensaje verde de éxito que dice algo como "Institución creada exitosamente"
- ✅ La página te redirige a la lista de instituciones
- ✅ En la lista, ahora ves "Hospital San José"
- ✅ La institución aparece con estado "Activo"

**📝 ANOTAR:** Guarda el ID o nombre de esta institución, la usarás en las siguientes pruebas.

---

### PRUEBA 2.2: Crear Usuario Staff (SuperAdmin)

**🎯 OBJETIVO:** Crear un usuario con rol Staff para probar permisos.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin**

2. **Ir a la sección de Usuarios**
   - En el menú, busca y haz click en "Usuarios" o "Users"
   - **QUÉ DEBERÍAS VER:** Una tabla con los usuarios existentes
   - Deberías ver al menos `admin@qlservices.com` y `admin@demo.com`

3. **Hacer click en "Crear" o "Nuevo Usuario"**
   - Busca el botón de crear (generalmente arriba a la derecha)
   - Haz click

4. **Llenar el formulario EXACTAMENTE así:**
   - **Correo Electrónico / Email:** `staff@hospital.com`
   - **Nombre / First Name:** `Juan`
   - **Apellido / Last Name:** `Pérez`
   - **Contraseña / Password:** `Staff@123456`
   - **Confirmar Contraseña / Confirm Password:** `Staff@123456`
   - **Rol / Role:** Selecciona "Staff" del dropdown
   - **Institución / Institution:** Selecciona "Hospital San José" del dropdown
     - **IMPORTANTE:** Como SuperAdmin, DEBES ver un dropdown con instituciones
     - Si NO ves el dropdown, hay un problema

5. **Hacer click en "Crear" o "Guardar"**

**✅ RESULTADO ESPERADO:**
- ✅ Mensaje de éxito: "Usuario creado exitosamente"
- ✅ Redirección a la lista de usuarios
- ✅ En la lista, ves el nuevo usuario `staff@hospital.com`
- ✅ El usuario muestra el rol "Staff"
- ✅ El usuario muestra la institución "Hospital San José"

**❌ SI VES UN ERROR:**
- Si dice "El campo Institución es requerido": Verifica que seleccionaste una institución del dropdown
- Si dice "Email ya existe": Usa un email diferente (ej: `staff2@hospital.com`)

---

### PRUEBA 2.3: Crear Usuario AdministrativeOperator (SuperAdmin)

**🎯 OBJETIVO:** Crear otro usuario de prueba con rol diferente.

**📋 PASOS DETALLADOS:**

1. **Seguir los mismos pasos que Prueba 2.2, pero con estos datos:**
   - **Email:** `operador@hospital.com`
   - **Nombre:** `María`
   - **Apellido:** `González`
   - **Contraseña:** `Operador@123456`
   - **Confirmar Contraseña:** `Operador@123456`
   - **Rol:** `AdministrativeOperator`
   - **Institución:** `Hospital San José`

**✅ RESULTADO ESPERADO:**
- ✅ Usuario creado exitosamente
- ✅ Aparece en la lista con rol "AdministrativeOperator"

---

### PRUEBA 2.4: Crear Usuario como InstitutionAdmin (SIN dropdown de institución)

**🎯 OBJETIVO:** Verificar que InstitutionAdmin NO ve el dropdown de instituciones al crear usuarios.

**📋 PASOS DETALLADOS:**

1. **Cerrar sesión de SuperAdmin**
   - Haz click en "Cerrar Sesión" o "Logout"

2. **Iniciar sesión como InstitutionAdmin**
   - Email: `admin@demo.com`
   - Contraseña: `Admin@123456`

3. **Ir a Usuarios**
   - En el menú, haz click en "Usuarios" o "Users"

4. **Hacer click en "Crear" o "Nuevo Usuario"**

5. **Revisar el formulario**
   - **QUÉ DEBERÍAS VER:**
     - Campo Email
     - Campo Nombre
     - Campo Apellido
     - Campo Contraseña
     - Campo Confirmar Contraseña
     - Campo Rol (dropdown)
     - **NO DEBE HABER:** Campo "Institución" o dropdown de instituciones

**✅ RESULTADO ESPERADO:**
- ✅ NO ves ningún campo o dropdown de "Institución"
- ✅ El formulario se ve más simple (sin el campo de institución)
- ✅ Puedes crear el usuario normalmente

6. **Crear el usuario con estos datos:**
   - **Email:** `staff@demo.com`
   - **Nombre:** `Pedro`
   - **Apellido:** `Sánchez`
   - **Contraseña:** `Staff@123456`
   - **Confirmar Contraseña:** `Staff@123456`
   - **Rol:** `Staff`
   - **NO hay campo Institución** (esto es correcto)

7. **Hacer click en "Crear"**

**✅ RESULTADO ESPERADO:**
- ✅ Usuario creado exitosamente
- ✅ El usuario se asigna automáticamente a "Empresa Demo" (institución del InstitutionAdmin)
- ✅ En la lista, el usuario aparece con institución "Empresa Demo"

---

### PRUEBA 2.5: Editar Usuario (SuperAdmin)

**🎯 OBJETIVO:** Verificar que puedes editar información de usuarios.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin**

2. **Ir a Usuarios**
   - En el menú, haz click en "Usuarios"

3. **Buscar el usuario a editar**
   - En la tabla, busca el usuario `staff@hospital.com` que creaste antes
   - En la misma fila, busca un botón que diga "Editar", "Edit", o un ícono de lápiz ✏️
   - Haz click en ese botón

4. **Verificar que el formulario carga con los datos actuales**
   - **QUÉ DEBERÍAS VER:**
     - El formulario se abre o carga en una nueva página
     - Los campos ya están llenos con los datos del usuario:
       - Email: `staff@hospital.com`
       - Nombre: `Juan`
       - Apellido: `Pérez`
       - Rol: `Staff`
       - Institución: `Hospital San José` (debe aparecer en dropdown)

5. **Modificar algunos campos**
   - Cambia el Nombre de `Juan` a `Juan Carlos`
   - Cambia el Apellido de `Pérez` a `Pérez López`
   - **NO cambies** el Email, Rol, ni Institución (para esta prueba)

6. **Guardar los cambios**
   - Busca el botón "Guardar", "Actualizar", "Save", o "Update"
   - Haz click
   - Espera 2-3 segundos

**✅ RESULTADO ESPERADO:**
- ✅ Mensaje de éxito: "Usuario actualizado exitosamente" o similar
- ✅ Redirección a la lista de usuarios
- ✅ En la lista, el usuario ahora muestra:
   - Nombre: `Juan Carlos`
   - Apellido: `Pérez López`
   - Email sigue siendo: `staff@hospital.com`

---

### PRUEBA 2.6: Eliminar Usuario (con confirmación)

**🎯 OBJETIVO:** Verificar que puedes eliminar usuarios con confirmación.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin**

2. **Ir a Usuarios**

3. **Buscar un usuario a eliminar**
   - Busca un usuario que hayas creado para pruebas (NO elimines `admin@qlservices.com` ni `admin@demo.com`)
   - En la fila del usuario, busca un botón que diga "Eliminar", "Delete", o un ícono de basura 🗑️
   - Haz click

4. **Verificar que aparece un diálogo de confirmación**
   - **QUÉ DEBERÍAS VER:**
     - Un popup o ventana emergente
     - Un mensaje que pregunta algo como: "¿Está seguro que desea eliminar este usuario?"
     - Dos botones: "Cancelar" / "Cancel" y "Eliminar" / "Delete" o "Confirmar" / "Confirm"

5. **Cancelar primero (para probar)**
   - Haz click en "Cancelar" o "Cancel"
   - **QUÉ DEBERÍAS VER:** El popup desaparece y sigues en la lista de usuarios
   - El usuario NO se eliminó

6. **Eliminar de verdad**
   - Haz click nuevamente en "Eliminar" del mismo usuario
   - En el popup de confirmación, haz click en "Eliminar" o "Confirmar"

**✅ RESULTADO ESPERADO:**
- ✅ El popup desaparece
- ✅ Mensaje de éxito: "Usuario eliminado exitosamente"
- ✅ El usuario desaparece de la lista
- ✅ Ya no puedes ver ese usuario en la tabla

---

### PRUEBA 2.7: Activar/Desactivar Usuario (ToggleActive)

**🎯 OBJETIVO:** Verificar que puedes activar y desactivar usuarios.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin**

2. **Ir a Usuarios**

3. **Buscar un usuario activo**
   - En la tabla, busca un usuario que tenga estado "Activo" o "Active"
   - En la misma fila, busca un botón que diga "Desactivar", "Deactivate", "ToggleActive", o un ícono de interruptor
   - Haz click

4. **Verificar el cambio**
   - Espera 2-3 segundos
   - **QUÉ DEBERÍAS VER:**
     - Un mensaje de éxito: "Usuario desactivado" o similar
     - En la tabla, el estado del usuario cambia a "Inactivo" o "Inactive"
     - O el botón cambia a "Activar"

5. **Activar nuevamente**
   - Haz click nuevamente en el botón del mismo usuario
   - **QUÉ DEBERÍAS VER:**
     - Mensaje: "Usuario activado" o similar
     - El estado vuelve a "Activo"

**✅ RESULTADO ESPERADO:**
- ✅ El estado cambia correctamente entre Activo e Inactivo
- ✅ Los mensajes de confirmación aparecen
- ✅ Los cambios se reflejan inmediatamente en la tabla

---

## 🏥 FASE 3: GESTIÓN DE ENTIDADES (EntityProfiles)

### PRUEBA 3.1: Crear Entidad CON Foto (SuperAdmin)

**🎯 OBJETIVO:** Crear una entidad con foto para probar la funcionalidad de subida de imágenes.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin**

2. **Ir a Entidades**
   - En el menú, busca y haz click en "Entidades", "Entity Profiles", o "Perfiles de Entidad"
   - **QUÉ DEBERÍAS VER:** Una tabla con las entidades existentes (puede estar vacía)

3. **Hacer click en "Crear" o "Nueva Entidad"**
   - Busca el botón de crear (arriba a la derecha)
   - Haz click

4. **Verificar que aparece el dropdown de Institución**
   - **QUÉ DEBERÍAS VER:**
     - Un campo o dropdown que dice "Institución" o "Institution"
     - Debes poder seleccionar de una lista de instituciones
     - **IMPORTANTE:** Como SuperAdmin, DEBES ver este dropdown
     - Si NO lo ves, hay un problema

5. **Llenar el formulario EXACTAMENTE así:**
   - **Institución:** Selecciona "Hospital San José" del dropdown
   - **Número de Identificación:** `8-123-4567`
   - **Nombre:** `Carlos`
   - **Apellido:** `Rodríguez`
   - **Correo Electrónico:** `carlos.rodriguez@example.com`
   - **Teléfono:** `507-6123-4567`
   - **Fecha de Nacimiento:** `15/03/1985` o selecciona del calendario
   - **Foto:** 
     - Busca un campo que diga "Foto", "Photo", o "Imagen"
     - Haz click en "Seleccionar archivo", "Choose File", o "Browse"
     - Selecciona una imagen de tu computadora (JPG o PNG, preferiblemente menor a 1MB)
     - **CONSEJO:** Usa una foto de perfil o avatar de prueba

6. **Hacer click en "Crear" o "Guardar"**
   - Espera 3-5 segundos (la subida de foto puede tardar)

**✅ RESULTADO ESPERADO:**
- ✅ Mensaje de éxito: "Entidad creada exitosamente"
- ✅ Redirección a la lista de entidades o a los detalles
- ✅ En la lista, ves la nueva entidad "Carlos Rodríguez"
- ✅ Si ves una miniatura, deberías ver la foto que subiste

**❌ SI VES UN ERROR:**
- Si dice "El campo Institución es requerido": Verifica que seleccionaste una institución
- Si dice "Formato de archivo no válido": Usa una imagen JPG o PNG
- Si dice "Archivo muy grande": Usa una imagen más pequeña (menos de 2MB)

---

### PRUEBA 3.2: Crear Entidad SIN Foto (SuperAdmin)

**🎯 OBJETIVO:** Crear una entidad sin foto para probar que el sistema funciona sin imágenes.

**📋 PASOS DETALLADOS:**

1. **Seguir los mismos pasos que Prueba 3.1, pero con estos datos:**
   - **Institución:** `Hospital San José`
   - **Número de Identificación:** `8-234-5678`
   - **Nombre:** `Ana`
   - **Apellido:** `Martínez`
   - **Correo Electrónico:** `ana.martinez@example.com`
   - **Teléfono:** `507-6234-5678`
   - **Fecha de Nacimiento:** `20/07/1990`
   - **Foto:** **NO subas ninguna foto** (deja el campo vacío)

2. **Hacer click en "Crear"**

**✅ RESULTADO ESPERADO:**
- ✅ Entidad creada exitosamente
- ✅ Aparece en la lista
- ✅ No hay error por falta de foto

---

### PRUEBA 3.3: Crear Entidad como InstitutionAdmin (SIN dropdown)

**🎯 OBJETIVO:** Verificar que InstitutionAdmin NO ve el dropdown de instituciones al crear entidades.

**📋 PASOS DETALLADOS:**

1. **Cerrar sesión de SuperAdmin**

2. **Iniciar sesión como InstitutionAdmin**
   - Email: `admin@demo.com`
   - Contraseña: `Admin@123456`

3. **Ir a Entidades**

4. **Hacer click en "Crear"**

5. **Revisar el formulario**
   - **QUÉ DEBERÍAS VER:**
     - Campo Número de Identificación
     - Campo Nombre
     - Campo Apellido
     - Campo Email
     - Campo Teléfono
     - Campo Fecha de Nacimiento
     - Campo Foto (si está habilitado)
     - **NO DEBE HABER:** Campo "Institución" o dropdown de instituciones

**✅ RESULTADO ESPERADO:**
- ✅ NO ves ningún campo de "Institución"
- ✅ El formulario se ve sin ese campo

6. **Crear la entidad con estos datos:**
   - **Número de Identificación:** `8-345-6789`
   - **Nombre:** `Luis`
   - **Apellido:** `Fernández`
   - **Correo Electrónico:** `luis.fernandez@example.com`
   - **Teléfono:** `507-6345-6789`
   - **Fecha de Nacimiento:** `10/05/1988`
   - **Foto:** Opcional (puedes subir una o no)

7. **Hacer click en "Crear"**

**✅ RESULTADO ESPERADO:**
- ✅ Entidad creada exitosamente
- ✅ La entidad se asigna automáticamente a "Empresa Demo"
- ✅ En la lista, la entidad aparece con institución "Empresa Demo"

---

### PRUEBA 3.4: Ver Detalles de Entidad

**🎯 OBJETIVO:** Verificar que puedes ver todos los detalles de una entidad.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como cualquier usuario autorizado**

2. **Ir a Entidades**

3. **Buscar una entidad en la lista**
   - En la tabla, busca una entidad (por ejemplo, "Carlos Rodríguez")

4. **Hacer click en "Ver", "Details", "Detalles", o el nombre de la entidad**
   - Generalmente hay un botón o el nombre es un enlace

5. **Revisar la página de detalles**
   - **QUÉ DEBERÍAS VER:**
     - Información completa de la entidad:
       - Nombre completo
       - Número de identificación
       - Email
       - Teléfono
       - Fecha de nacimiento
       - **Nombre de la Institución** (debe aparecer)
       - **Foto de la entidad** (si tiene foto, debe mostrarse)
     - Sección de "Tarjetas Asociadas" (puede estar vacía)
     - Sección de "Eventos Asociados" (puede estar vacía)
     - Botones de acción: "Editar", "Eliminar", "ToggleActive"

**✅ RESULTADO ESPERADO:**
- ✅ Toda la información se muestra correctamente
- ✅ Si la entidad tiene foto, se muestra la foto
- ✅ Si la entidad NO tiene foto, se muestra un placeholder o nada
- ✅ El nombre de la institución es visible

---

### PRUEBA 3.5: Editar Entidad (SuperAdmin - cambiar institución)

**🎯 OBJETIVO:** Verificar que SuperAdmin puede editar entidades y cambiar su institución.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin**

2. **Ir a Entidades**

3. **Buscar la entidad "Carlos Rodríguez"**
   - En la tabla, busca esa entidad
   - Haz click en "Editar" o "Edit"

4. **Verificar que el formulario carga con los datos**
   - **QUÉ DEBERÍAS VER:**
     - Los campos están llenos con los datos actuales
     - El dropdown de "Institución" muestra "Hospital San José" seleccionado

5. **Cambiar la institución**
   - En el dropdown de "Institución", selecciona "Empresa Demo"
   - **NO cambies** otros campos (para esta prueba)

6. **Guardar los cambios**
   - Haz click en "Guardar" o "Actualizar"
   - Espera 2-3 segundos

**✅ RESULTADO ESPERADO:**
- ✅ Mensaje de éxito: "Entidad actualizada exitosamente"
- ✅ Redirección a la lista o detalles
- ✅ Si ves los detalles, la institución ahora es "Empresa Demo"

---

### PRUEBA 3.6: Editar Entidad (InstitutionAdmin - NO puede cambiar institución)

**🎯 OBJETIVO:** Verificar que InstitutionAdmin NO puede cambiar la institución de una entidad.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como InstitutionAdmin**

2. **Ir a Entidades**

3. **Buscar una entidad de "Empresa Demo"**
   - Busca la entidad "Luis Fernández" que creaste antes
   - Haz click en "Editar"

4. **Revisar el formulario**
   - **QUÉ DEBERÍAS VER:**
     - Los campos están llenos
     - **NO DEBE HABER:** Campo "Institución" o dropdown de instituciones
     - El formulario NO tiene opción para cambiar la institución

5. **Modificar otro campo (opcional)**
   - Cambia el teléfono a `507-6456-7890`
   - Guarda los cambios

**✅ RESULTADO ESPERADO:**
- ✅ Cambios guardados exitosamente
- ✅ La institución NO cambia (permanece "Empresa Demo")
- ✅ El teléfono se actualiza

---

### PRUEBA 3.7: Eliminar Entidad

**🎯 OBJETIVO:** Verificar que puedes eliminar entidades con confirmación.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin o InstitutionAdmin**

2. **Ir a Entidades**

3. **Buscar una entidad de prueba para eliminar**
   - Busca una entidad que hayas creado específicamente para pruebas
   - **NO elimines** entidades importantes

4. **Hacer click en "Eliminar" o "Delete"**
   - En la fila de la entidad, busca el botón de eliminar
   - Haz click

5. **Confirmar la eliminación**
   - Debe aparecer un popup de confirmación
   - Haz click en "Eliminar" o "Confirmar"

**✅ RESULTADO ESPERADO:**
- ✅ Mensaje de éxito: "Entidad eliminada exitosamente"
- ✅ La entidad desaparece de la lista
- ✅ Ya no puedes ver esa entidad

---

### PRUEBA 3.8: Activar/Desactivar Entidad (ToggleActive)

**🎯 OBJETIVO:** Verificar que puedes activar y desactivar entidades.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin o InstitutionAdmin**

2. **Ir a Entidades**

3. **Buscar una entidad activa**
   - En la tabla, busca una entidad con estado "Activo"

4. **Hacer click en "ToggleActive" o "Desactivar"**
   - Busca el botón de activar/desactivar
   - Haz click

5. **Verificar el cambio**
   - Espera 2-3 segundos
   - **QUÉ DEBERÍAS VER:**
     - Mensaje de éxito
     - El estado cambia a "Inactivo" en la tabla
     - O el botón cambia a "Activar"

6. **Activar nuevamente**
   - Haz click nuevamente en el botón
   - **QUÉ DEBERÍAS VER:**
     - Mensaje de éxito
     - El estado vuelve a "Activo"

**✅ RESULTADO ESPERADO:**
- ✅ El estado cambia correctamente
- ✅ Los mensajes aparecen
- ✅ Los cambios se reflejan inmediatamente

---

## 🎴 FASE 4: GESTIÓN DE TARJETAS (Cards)

**⚠️ IMPORTANTE:** Las tarjetas NO se pueden editar (son inmutables). Solo se pueden crear, ver, eliminar y activar/desactivar.

### PRUEBA 4.1: Crear Tarjeta para Entidad con Foto (SuperAdmin)

**🎯 OBJETIVO:** Crear una tarjeta para una entidad que tiene foto.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin**

2. **Ir a Tarjetas**
   - En el menú, busca y haz click en "Tarjetas", "Cards", o "Carnets"
   - **QUÉ DEBERÍAS VER:** Una tabla con las tarjetas existentes (puede estar vacía)

3. **Hacer click en "Crear" o "Nueva Tarjeta"**
   - Busca el botón de crear
   - Haz click

4. **Verificar la lista de entidades disponibles**
   - **QUÉ DEBERÍAS VER:**
     - Un dropdown o lista de entidades
     - **IMPORTANTE:** Solo deben aparecer entidades que:
       - Están activas (IsActive = true)
       - NO tienen una tarjeta activa ya
     - Como SuperAdmin, deberías ver entidades de TODAS las instituciones
     - Deberías ver "Carlos Rodríguez" (la entidad con foto que creaste)

5. **Filtro por Institución (si está disponible)**
   - Si hay un filtro o dropdown de "Institución", selecciona "Hospital San José"
   - Esto debería filtrar la lista para mostrar solo entidades de esa institución

6. **Seleccionar una entidad**
   - En el dropdown o lista, selecciona "Carlos Rodríguez"
   - **QUÉ DEBERÍAS VER:**
     - El nombre de la entidad
     - El nombre de la institución junto al nombre (ej: "Carlos Rodríguez - Hospital San José")

7. **Hacer click en "Crear" o "Generar Tarjeta"**
   - Espera 3-5 segundos (la generación del QR puede tardar)

**✅ RESULTADO ESPERADO:**
- ✅ Mensaje de éxito: "Tarjeta creada exitosamente"
- ✅ Redirección a la lista de tarjetas o a los detalles
- ✅ En la lista, ves la nueva tarjeta asociada a "Carlos Rodríguez"
- ✅ La tarjeta tiene un número único
- ✅ La tarjeta está en estado "Activo"

---

### PRUEBA 4.2: Crear Tarjeta para Entidad sin Foto (SuperAdmin)

**🎯 OBJETIVO:** Crear una tarjeta para una entidad sin foto.

**📋 PASOS DETALLADOS:**

1. **Seguir los mismos pasos que Prueba 4.1, pero:**
   - Selecciona la entidad "Ana Martínez" (la que creaste sin foto)

**✅ RESULTADO ESPERADO:**
- ✅ Tarjeta creada exitosamente
- ✅ Aparece en la lista
- ✅ No hay error por falta de foto

---

### PRUEBA 4.3: Crear Tarjeta como InstitutionAdmin (solo su institución)

**🎯 OBJETIVO:** Verificar que InstitutionAdmin solo puede crear tarjetas para entidades de su institución.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como InstitutionAdmin**

2. **Ir a Tarjetas**

3. **Hacer click en "Crear"**

4. **Revisar la lista de entidades disponibles**
   - **QUÉ DEBERÍAS VER:**
     - Solo entidades de "Empresa Demo"
     - NO deberías ver "Carlos Rodríguez" ni "Ana Martínez" (son de "Hospital San José")
     - Deberías ver "Luis Fernández" (si lo creaste)

5. **Seleccionar una entidad de "Empresa Demo"**
   - Selecciona "Luis Fernández" o cualquier otra entidad de tu institución
   - Crear la tarjeta

**✅ RESULTADO ESPERADO:**
- ✅ Tarjeta creada exitosamente
- ✅ Solo puedes ver y crear tarjetas para entidades de tu institución

---

### PRUEBA 4.4: Ver Detalles de Tarjeta

**🎯 OBJETIVO:** Verificar que puedes ver todos los detalles de una tarjeta.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como cualquier usuario autorizado**

2. **Ir a Tarjetas**

3. **Buscar una tarjeta en la lista**
   - Busca la tarjeta de "Carlos Rodríguez"

4. **Hacer click en "Ver", "Details", o el número de tarjeta**

5. **Revisar la página de detalles**
   - **QUÉ DEBERÍAS VER:**
     - Información de la tarjeta:
       - Número de tarjeta
       - Estado (Activo/Inactivo)
       - Fecha de creación
     - Información de la entidad asociada:
       - Nombre completo
       - Foto (si tiene)
       - Institución
     - **QR Code:**
       - Debe mostrarse una imagen del código QR
       - El QR debe ser escaneable
     - Botones de acción:
       - "Imprimir" o "Print"
       - "Eliminar"
       - "ToggleActive"

**✅ RESULTADO ESPERADO:**
- ✅ Toda la información se muestra correctamente
- ✅ El QR code es visible y escaneable
- ✅ Los botones de acción están disponibles

---

### PRUEBA 4.5: Vista de Impresión - Tarjeta CON Foto

**🎯 OBJETIVO:** Verificar que la vista de impresión muestra correctamente la foto en el frente.

**📋 PASOS DETALLADOS:**

1. **Estar en los detalles de una tarjeta con foto**
   - Ve a los detalles de la tarjeta de "Carlos Rodríguez"

2. **Hacer click en "Imprimir", "Print", o "Vista de Impresión"**
   - Busca el botón de imprimir
   - Haz click

3. **Revisar la vista de impresión**
   - **QUÉ DEBERÍAS VER:**
     - **Vista Frontal (Front):**
       - Logo o nombre de la institución en la parte superior
       - Número de tarjeta
       - Nombre de la entidad: "Carlos Rodríguez"
       - **FOTO de la entidad** (debe mostrarse claramente)
       - Diseño profesional y ordenado
     - **Vista Trasera (Back):**
       - **QR Code** (debe mostrarse claramente)
       - Información de contacto de la institución
       - Instrucciones o información adicional

4. **Verificar dimensiones (opcional)**
   - Presiona `Ctrl + P` para abrir el diálogo de impresión
   - Verifica que las dimensiones son aproximadamente 85.6mm x 54mm (tamaño CR80)
   - Cierra el diálogo de impresión (no imprimas realmente)

**✅ RESULTADO ESPERADO:**
- ✅ La foto se muestra en el frente de la tarjeta
- ✅ El QR code se muestra en el reverso
- ✅ El diseño es profesional y legible
- ✅ Las dimensiones son correctas para impresión

---

### PRUEBA 4.6: Vista de Impresión - Tarjeta SIN Foto

**🎯 OBJETIVO:** Verificar que la vista de impresión muestra un placeholder cuando no hay foto.

**📋 PASOS DETALLADOS:**

1. **Estar en los detalles de una tarjeta sin foto**
   - Ve a los detalles de la tarjeta de "Ana Martínez"

2. **Hacer click en "Imprimir" o "Vista de Impresión"**

3. **Revisar la vista frontal**
   - **QUÉ DEBERÍAS VER:**
     - Logo o nombre de la institución
     - Número de tarjeta
     - Nombre de la entidad: "Ana Martínez"
     - **En lugar de foto:**
       - Un placeholder agradable (puede ser un ícono, un avatar genérico, o un espacio con borde)
       - **NO debe haber un espacio vacío feo**
       - El placeholder debe verse profesional

**✅ RESULTADO ESPERADO:**
- ✅ Se muestra un placeholder visualmente agradable
- ✅ El diseño mantiene las proporciones correctas
- ✅ El QR code sigue en el reverso
- ✅ La tarjeta se ve completa y profesional

---

### PRUEBA 4.7: Validación - No Crear Múltiples Tarjetas Activas

**🎯 OBJETIVO:** Verificar que una entidad no puede tener múltiples tarjetas activas.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como cualquier usuario autorizado**

2. **Ir a Tarjetas**

3. **Hacer click en "Crear"**

4. **Revisar la lista de entidades disponibles**
   - **QUÉ DEBERÍAS VER:**
     - Solo entidades que NO tienen tarjeta activa
     - **NO deberías ver:** "Carlos Rodríguez" (ya tiene tarjeta activa)
     - **SÍ deberías ver:** "Ana Martínez" (si no tiene tarjeta activa aún)

5. **Intentar crear otra tarjeta para "Carlos Rodríguez" (si aparece)**
   - Si por error aparece en la lista, intenta seleccionarla y crear
   - **QUÉ DEBERÍAS VER:**
     - Un mensaje de error: "Esta entidad ya tiene una tarjeta activa"
     - O la tarjeta NO se crea

**✅ RESULTADO ESPERADO:**
- ✅ Las entidades con tarjeta activa NO aparecen en la lista
- ✅ O muestran un error claro si se intenta crear

---

### PRUEBA 4.8: Eliminar Tarjeta

**🎯 OBJETIVO:** Verificar que puedes eliminar tarjetas con confirmación.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin o InstitutionAdmin**

2. **Ir a Tarjetas**

3. **Buscar una tarjeta de prueba para eliminar**
   - Busca una tarjeta que hayas creado específicamente para pruebas

4. **Hacer click en "Eliminar" o "Delete"**

5. **Confirmar la eliminación**
   - Debe aparecer un popup de confirmación
   - Haz click en "Eliminar" o "Confirmar"

**✅ RESULTADO ESPERADO:**
- ✅ Mensaje de éxito: "Tarjeta eliminada exitosamente"
- ✅ La tarjeta desaparece de la lista
- ✅ Ahora puedes crear una nueva tarjeta para esa entidad (porque ya no tiene tarjeta activa)

---

### PRUEBA 4.9: Activar/Desactivar Tarjeta (ToggleActive)

**🎯 OBJETIVO:** Verificar que puedes activar y desactivar tarjetas.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin o InstitutionAdmin**

2. **Ir a Tarjetas**

3. **Buscar una tarjeta activa**
   - En la tabla, busca una tarjeta con estado "Activo"

4. **Hacer click en "ToggleActive" o "Desactivar"**
   - Busca el botón de activar/desactivar
   - Haz click

5. **Verificar el cambio**
   - Espera 2-3 segundos
   - **QUÉ DEBERÍAS VER:**
     - Mensaje de éxito
     - El estado cambia a "Inactivo" en la tabla

6. **Activar nuevamente**
   - Haz click nuevamente en el botón
   - **QUÉ DEBERÍAS VER:**
     - Mensaje de éxito
     - El estado vuelve a "Activo"

**✅ RESULTADO ESPERADO:**
- ✅ El estado cambia correctamente
- ✅ Los mensajes aparecen
- ✅ Los cambios se reflejan inmediatamente

---

## 📅 FASE 5: GESTIÓN DE EVENTOS (Events)

### PRUEBA 5.1: Crear Evento (SuperAdmin)

**🎯 OBJETIVO:** Crear un evento para una entidad.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin**

2. **Ir a Eventos**
   - En el menú, busca y haz click en "Eventos", "Events", o "Registros de Eventos"
   - **QUÉ DEBERÍAS VER:** Una tabla con los eventos existentes (puede estar vacía)

3. **Hacer click en "Crear" o "Nuevo Evento"**
   - Busca el botón de crear
   - Haz click

4. **Verificar la lista de entidades**
   - **QUÉ DEBERÍAS VER:**
     - Un dropdown o lista de entidades
     - Como SuperAdmin, deberías ver entidades de TODAS las instituciones
     - **Filtro por Institución (si está disponible):**
       - Si hay un filtro, selecciona "Hospital San José"
       - Esto debería filtrar la lista

5. **Llenar el formulario EXACTAMENTE así:**
   - **Entidad:** Selecciona "Carlos Rodríguez" del dropdown
   - **Tipo de Evento:** Selecciona un tipo (ej: "Consulta", "Cita", "Revisión")
   - **Fecha:** Selecciona una fecha futura (ej: mañana o la próxima semana)
   - **Hora:** Selecciona una hora (ej: 10:00 AM)
   - **Descripción:** `Consulta médica de rutina para seguimiento`
   - **Estado:** Debe estar en "Scheduled" o "Programado" por defecto

6. **Hacer click en "Crear" o "Guardar"**

**✅ RESULTADO ESPERADO:**
- ✅ Mensaje de éxito: "Evento creado exitosamente"
- ✅ Redirección a la lista de eventos
- ✅ En la lista, ves el nuevo evento asociado a "Carlos Rodríguez"
- ✅ El evento muestra la institución correcta (automáticamente desde la entidad)

**❌ SI VES UN ERROR:**
- Si dice "El campo Institución es requerido": Esto es un bug, debe asignarse automáticamente
- Si dice "El campo EntityProfile es requerido": Verifica que seleccionaste una entidad

---

### PRUEBA 5.2: Crear Evento como InstitutionAdmin (solo su institución)

**🎯 OBJETIVO:** Verificar que InstitutionAdmin solo puede crear eventos para entidades de su institución.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como InstitutionAdmin**

2. **Ir a Eventos**

3. **Hacer click en "Crear"**

4. **Revisar la lista de entidades disponibles**
   - **QUÉ DEBERÍAS VER:**
     - Solo entidades de "Empresa Demo"
     - NO deberías ver "Carlos Rodríguez" ni "Ana Martínez" (son de "Hospital San José")
     - Deberías ver "Luis Fernández" (si lo creaste)

5. **Seleccionar una entidad de "Empresa Demo"**
   - Selecciona "Luis Fernández"
   - Llena el resto del formulario:
     - **Tipo de Evento:** Selecciona un tipo
     - **Fecha:** Selecciona una fecha futura
     - **Hora:** Selecciona una hora
     - **Descripción:** `Evento de prueba para InstitutionAdmin`

6. **Hacer click en "Crear"**

**✅ RESULTADO ESPERADO:**
- ✅ Evento creado exitosamente
- ✅ El evento se asigna automáticamente a "Empresa Demo"
- ✅ Solo puedes crear eventos para entidades de tu institución

---

### PRUEBA 5.3: Editar Evento

**🎯 OBJETIVO:** Verificar que puedes editar eventos (solo eventos programados).

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin o InstitutionAdmin**

2. **Ir a Eventos**

3. **Buscar un evento programado (Scheduled)**
   - En la tabla, busca un evento con estado "Scheduled" o "Programado"
   - Haz click en "Editar" o "Edit"

4. **Verificar que el formulario carga con los datos**
   - **QUÉ DEBERÍAS VER:**
     - Los campos están llenos con los datos actuales del evento

5. **Modificar algunos campos**
   - Cambia la descripción a: `Evento modificado - nueva descripción`
   - Cambia la hora a una hora diferente
   - **NO cambies** la entidad ni la fecha (para esta prueba)

6. **Guardar los cambios**
   - Haz click en "Guardar" o "Actualizar"

**✅ RESULTADO ESPERADO:**
- ✅ Mensaje de éxito: "Evento actualizado exitosamente"
- ✅ Los cambios se reflejan en la lista
- ✅ La descripción y hora se actualizaron

---

### PRUEBA 5.4: Eliminar Evento

**🎯 OBJETIVO:** Verificar que puedes eliminar eventos con confirmación.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin o InstitutionAdmin**

2. **Ir a Eventos**

3. **Buscar un evento de prueba para eliminar**
   - Busca un evento que hayas creado específicamente para pruebas

4. **Hacer click en "Eliminar" o "Delete"**

5. **Confirmar la eliminación**
   - Debe aparecer un popup de confirmación
   - Haz click en "Eliminar" o "Confirmar"

**✅ RESULTADO ESPERADO:**
- ✅ Mensaje de éxito: "Evento eliminado exitosamente"
- ✅ El evento desaparece de la lista

---

### PRUEBA 5.5: Activar/Desactivar Evento (ToggleActive)

**🎯 OBJETIVO:** Verificar que puedes cambiar el estado de eventos.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin o InstitutionAdmin**

2. **Ir a Eventos**

3. **Buscar un evento programado (Scheduled)**
   - En la tabla, busca un evento con estado "Scheduled"

4. **Hacer click en "ToggleActive" o el botón de estado**
   - Haz click

5. **Verificar el cambio**
   - Espera 2-3 segundos
   - **QUÉ DEBERÍAS VER:**
     - Mensaje de éxito
     - El estado cambia a "NotCompleted" o "No Completado"

6. **Cambiar nuevamente**
   - Haz click nuevamente en el botón
   - **QUÉ DEBERÍAS VER:**
     - Mensaje de éxito
     - El estado vuelve a "Scheduled"

**✅ RESULTADO ESPERADO:**
- ✅ El estado cambia correctamente entre "Scheduled" y "NotCompleted"
- ✅ Los mensajes aparecen
- ✅ Los cambios se reflejan inmediatamente

---

## 🔍 FASE 6: VALIDACIÓN DE MULTI-TENANCY

### PRUEBA 6.1: SuperAdmin ve TODAS las instituciones

**🎯 OBJETIVO:** Verificar que SuperAdmin puede ver datos de todas las instituciones.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin**

2. **Ir a diferentes secciones y verificar:**
   - **Entidades:** Deberías ver entidades de "Hospital San José" Y "Empresa Demo"
   - **Tarjetas:** Deberías ver tarjetas de ambas instituciones
   - **Eventos:** Deberías ver eventos de ambas instituciones
   - **Usuarios:** Deberías ver usuarios de ambas instituciones

3. **En cada sección, verifica:**
   - En la tabla, busca la columna "Institución" o "Institution"
   - **QUÉ DEBERÍAS VER:**
     - Algunas filas muestran "Hospital San José"
     - Otras filas muestran "Empresa Demo"
     - Ves datos de AMBAS instituciones

**✅ RESULTADO ESPERADO:**
- ✅ En todas las secciones ves datos de todas las instituciones
- ✅ Puedes identificar a qué institución pertenece cada registro
- ✅ Los dropdowns de creación muestran todas las instituciones

---

### PRUEBA 6.2: InstitutionAdmin solo ve SU institución

**🎯 OBJETIVO:** Verificar que InstitutionAdmin solo ve datos de su institución.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como InstitutionAdmin**

2. **Ir a diferentes secciones y verificar:**
   - **Entidades:** Solo deberías ver entidades de "Empresa Demo"
   - **Tarjetas:** Solo deberías ver tarjetas de "Empresa Demo"
   - **Eventos:** Solo deberías ver eventos de "Empresa Demo"
   - **Usuarios:** Solo deberías ver usuarios de "Empresa Demo"

3. **En cada sección, verifica:**
   - **QUÉ DEBERÍAS VER:**
     - Todas las filas muestran "Empresa Demo" (o no muestran institución porque es la única)
     - **NO ves:** "Hospital San José" en ninguna parte
     - **NO ves:** Entidades, tarjetas, eventos o usuarios de otras instituciones

**✅ RESULTADO ESPERADO:**
- ✅ En todas las secciones solo ves datos de "Empresa Demo"
- ✅ No ves datos de otras instituciones
- ✅ Los formularios de creación NO tienen dropdown de institución

---

### PRUEBA 6.3: Intentar Acceder a Datos de Otra Institución (InstitutionAdmin)

**🎯 OBJETIVO:** Verificar que InstitutionAdmin NO puede acceder a datos de otras instituciones.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como SuperAdmin primero**
   - Anota el ID de una entidad de "Hospital San José"
   - Puedes verlo en la URL cuando estás en los detalles: `/EntityProfiles/Details/{id}`

2. **Cerrar sesión y loguearte como InstitutionAdmin**

3. **Intentar acceder directamente**
   - En la barra de direcciones, escribe: `http://164.68.99.83/EntityProfiles/Details/{id-de-hospital-san-jose}`
   - Reemplaza `{id-de-hospital-san-jose}` con el ID real que anotaste
   - Presiona `Enter`

**✅ RESULTADO ESPERADO:**
- ✅ Ves una página de error 404 (No encontrado)
- ✅ O ves "Acceso Denegado"
- ✅ O te redirige al Dashboard
- ✅ **NO ves** la información de la entidad de "Hospital San José"

---

## 🖨️ FASE 7: IMPRESIÓN Y QR CODES

### PRUEBA 7.1: Escanear QR Code

**🎯 OBJETIVO:** Verificar que el QR code es escaneable y funciona correctamente.

**📋 PASOS DETALLADOS:**

1. **Estar logueado como cualquier usuario autorizado**

2. **Ir a Tarjetas**

3. **Ver detalles de una tarjeta**
   - Busca una tarjeta
   - Haz click en "Ver" o "Details"

4. **Encontrar el QR code**
   - **QUÉ DEBERÍAS VER:**
     - Una imagen cuadrada con un patrón de cuadros negros y blancos
     - Es el código QR

5. **Escanear el QR code**
   - Usa tu teléfono móvil
   - Abre la aplicación de cámara (o una app de escáner QR)
   - Apunta la cámara al QR code en la pantalla
   - Espera a que lo escanee

6. **Verificar la URL a la que redirige**
   - **QUÉ DEBERÍAS VER:**
     - El teléfono muestra una URL
     - La URL debería ser algo como: `http://164.68.99.83/Qr/Scan/{id}` o similar
     - Al abrir la URL, debería mostrar información de la tarjeta/entidad

**✅ RESULTADO ESPERADO:**
- ✅ El QR code es escaneable
- ✅ La URL es correcta
- ✅ Al abrir la URL, se muestra información válida

---

### PRUEBA 7.2: Vista de Impresión Completa

**🎯 OBJETIVO:** Verificar que la vista de impresión es perfecta para imprimir.

**📋 PASOS DETALLADOS:**

1. **Estar en los detalles de una tarjeta**

2. **Hacer click en "Imprimir" o "Vista de Impresión"**

3. **Revisar la vista completa**
   - **QUÉ DEBERÍAS VER:**
     - **Frente de la tarjeta:**
       - Diseño limpio y profesional
       - Logo/nombre de institución visible
       - Número de tarjeta legible
       - Nombre de entidad destacado
       - Foto o placeholder bien posicionado
     - **Reverso de la tarjeta:**
       - QR code grande y claro
       - Información de contacto
       - Texto legible

4. **Abrir vista de impresión del navegador**
   - Presiona `Ctrl + P` (Windows) o `Cmd + P` (Mac)
   - **QUÉ DEBERÍAS VER:**
     - El diálogo de impresión se abre
     - La vista previa muestra la tarjeta completa
     - Las dimensiones son correctas (aproximadamente 85.6mm x 54mm)
     - Todo el contenido es visible y legible

5. **Cerrar el diálogo de impresión**
   - Haz click en "Cancelar" (no imprimas realmente)

**✅ RESULTADO ESPERADO:**
- ✅ La vista de impresión es profesional
- ✅ Todo el contenido es legible
- ✅ Las dimensiones son correctas
- ✅ El diseño es adecuado para impresión física

---

## ✅ CHECKLIST FINAL DE VALIDACIÓN

### Por Rol - Marca con ✅ cuando completes cada prueba:

#### SuperAdmin
- [ ] Puede iniciar sesión
- [ ] Ve menú completo (incluye Instituciones)
- [ ] Puede crear usuarios en cualquier institución
- [ ] Ve dropdown de instituciones al crear usuarios
- [ ] Puede crear entidades en cualquier institución
- [ ] Ve dropdown de instituciones al crear entidades
- [ ] Puede crear tarjetas para cualquier entidad
- [ ] Ve entidades de todas las instituciones al crear tarjetas
- [ ] Puede crear eventos para cualquier entidad
- [ ] Ve entidades de todas las instituciones al crear eventos
- [ ] Puede editar y cambiar institución de entidades
- [ ] Ve datos de todas las instituciones en todas las secciones

#### InstitutionAdmin
- [ ] Puede iniciar sesión
- [ ] NO ve opción "Instituciones" en el menú
- [ ] Puede crear usuarios (sin dropdown de institución)
- [ ] Los usuarios se asignan automáticamente a su institución
- [ ] Puede crear entidades (sin dropdown de institución)
- [ ] Las entidades se asignan automáticamente a su institución
- [ ] Solo ve entidades de su institución al crear tarjetas
- [ ] Solo ve entidades de su institución al crear eventos
- [ ] NO puede cambiar institución al editar
- [ ] Solo ve datos de su institución en todas las secciones
- [ ] NO puede acceder a datos de otras instituciones

#### Funcionalidades CRUD
- [ ] Crear funciona en todos los módulos
- [ ] Editar funciona en todos los módulos (excepto Cards)
- [ ] Eliminar funciona en todos los módulos (con confirmación)
- [ ] ToggleActive funciona en todos los módulos
- [ ] Ver detalles funciona en todos los módulos

#### Validaciones
- [ ] Campos requeridos se validan correctamente
- [ ] Fotos se validan (tipo, tamaño)
- [ ] Una entidad no puede tener múltiples tarjetas activas
- [ ] Mensajes de error son claros

#### Impresión
- [ ] Vista de impresión muestra foto cuando existe
- [ ] Vista de impresión muestra placeholder cuando no hay foto
- [ ] QR code se muestra correctamente
- [ ] Dimensiones son correctas para impresión
- [ ] QR code es escaneable

---

## 📝 NOTAS IMPORTANTES PARA EL TESTER

### Antes de Comenzar:
1. **Limpia las cookies del navegador** o usa modo incógnito
2. **Ten a mano las credenciales:**
   - SuperAdmin: `admin@qlservices.com` / `Admin@123456`
   - InstitutionAdmin: `admin@demo.com` / `Admin@123456`

### Durante las Pruebas:
1. **Anota cualquier error** que veas
2. **Toma capturas de pantalla** si algo no funciona como se espera
3. **Verifica los mensajes** que aparecen (éxito o error)
4. **Revisa que los datos se guarden** correctamente

### Si Algo No Funciona:
1. **Verifica que estás usando el rol correcto** para esa prueba
2. **Limpia las cookies** y vuelve a intentar
3. **Verifica que los datos de prueba existen** (institución, entidades, etc.)
4. **Revisa la consola del navegador** (F12 → Console) para ver errores JavaScript

### Datos de Prueba a Crear Primero (con SuperAdmin):
1. Institución: "Hospital San José"
2. Usuario Staff: `staff@hospital.com`
3. Usuario AdministrativeOperator: `operador@hospital.com`
4. Entidad con foto: "Carlos Rodríguez"
5. Entidad sin foto: "Ana Martínez"

---

**Fecha de Creación:** 17 de Enero, 2026  
**Versión:** 2.0 - Detallado para Testers Sin Conocimiento Técnico  
**Estado:** Listo para Ejecución
