# FixHub — Reporte QA desde la perspectiva de usuario real

**Enfoque:** Persona usando la herramienta, no desarrollador.  
**Objetivo:** Detectar confusión, fricción, errores funcionales, mensajes poco claros y estados ilógicos.  
**Fuente:** Comportamiento real del sistema según documentación y código (sin inventar funcionalidades).

---

# FASE 1 — Entender el sistema como persona

## ¿Qué es FixHub?

FixHub es una **plataforma para pedir servicios del hogar** (reparaciones, instalaciones, etc.). No es un marketplace donde tú eliges al técnico de una lista: es una **empresa de servicios** que recibe tu solicitud y **ella misma asigna** a un técnico. Tú pides, la empresa organiza, un técnico va a tu casa.

## ¿Para quién es?

- **Clientes:** Personas que necesitan que les arreglen algo en casa (aire acondicionado, cerrajería, etc.).
- **Técnicos:** Profesionales que quieren recibir trabajos asignados por la empresa.
- **Administradores:** Personal de la empresa que ve todas las solicitudes, asigna técnicos y resuelve incidencias.

## ¿Qué problema resuelve?

Para el **cliente:** poder pedir un servicio desde casa, seguir el estado de su solicitud y, al final, confirmar que el trabajo se hizo y calificar.  
Para el **técnico:** ver trabajos disponibles, enviar su propuesta (precio, mensaje) y recibir asignaciones cuando la empresa lo elige.  
Para la **empresa:** centralizar solicitudes, elegir qué técnico va a cada trabajo y llevar control operativo.

## ¿Qué espera hacer un cliente cuando entra?

1. Registrarse o entrar con su cuenta.
2. Crear una **solicitud** (qué necesita, dónde, presupuesto aproximado).
3. Ver **sus solicitudes** y el estado (esperando, asignado, en curso, terminado).
4. En un mundo ideal: **ver quién le ha hecho propuestas** para elegir o al menos informarse.
5. Que le avisen cuando ya hay un técnico asignado.
6. Cuando el técnico termina, **confirmar que todo está bien** y marcar como completado.
7. **Calificar** al técnico (estrellas y comentario).

**Lo que no está claro desde fuera:** En FixHub el cliente **no elige** la propuesta. Quien asigna es siempre un **administrador**. Si el cliente espera “ver propuestas y aceptar una”, se va a frustrar: no puede aceptar; solo puede ver (y hoy ni eso funciona bien — ver más abajo).

## ¿Qué espera hacer un técnico?

1. Registrarse como técnico.
2. Entrar y ver **trabajos disponibles** (solicitudes abiertas).
3. **Enviar su propuesta** (precio, mensaje) a los que le interesen.
4. Esperar a que la **empresa lo asigne** (un admin acepta una propuesta).
5. Ver **sus asignaciones** y **marcar que ya inició** el trabajo en sitio.
6. Hacer el trabajo.
7. Que el **cliente confirme** que está terminado (el técnico no “completa” el trabajo en la app; lo hace el cliente).

**Lo que no está claro:** Un técnico nuevo queda en estado “Pending” hasta que un admin lo **apruebe**. Si no lo aprueban, no podrá ser asignado a ningún trabajo. Eso puede no estar explicado en pantalla y genera “me registré y no me sale ningún trabajo”.

---

# FASE 2 — Prueba como cliente real (Juan Pérez)

**Contexto:** Juan Pérez. Se le dañó el aire acondicionado. Nivel técnico bajo. Urgencia alta.

## 1. Registro

- **Qué hace:** Se registra con nombre, email, contraseña. Elige “Cliente” (o el equivalente).
- **Claridad:** Depende de la pantalla. Si pide “Rol” o un número (1=Cliente), puede confundir. Lo ideal es que diga algo como “Soy cliente / Necesito un servicio”.
- **Problema grave conocido:** Si en el registro se puede elegir “Admin”, cualquiera puede hacerse administrador. Eso es un fallo de seguridad; como usuario normal no lo notarías, pero rompe la confianza en el sistema.

## 2. Login

- **Qué hace:** Entra con email y contraseña.
- **Claridad:** Habitual. Si la contraseña falla, el mensaje debe ser claro (“Contraseña incorrecta”) y no técnico.

## 3. Crear solicitud

- **Qué hace:** Completa un formulario: qué necesita (categoría/tipo de servicio), título, descripción, dirección, quizá presupuesto mínimo/máximo.
- **Posible confusión:** Si “categoría” es un número (1, 2, 3) y no una lista legible (“Aire acondicionado”, “Cerrajería”), Juan no sabrá qué poner. No está confirmado si la web muestra un desplegable de categorías o pide un ID.
- **Urgencia:** No se ve opción de “urgente” o “prioridad”. Para alguien con el aire dañado, no hay forma explícita de comunicar urgencia.

## 4. Ver mi solicitud

- **Qué hace:** Entra a “Mis solicitudes” o similar y ve la que acaba de crear.
- **Claridad:** Debería ver estado (“Abierta”, “En espera”, etc.). Si solo ve “Open” en inglés o un código, no es humano.
- **Confianza:** Ver que su solicitud está ahí y tiene un estado claro da tranquilidad.

## 5. Esperar técnico

- **Qué hace:** Espera. Recibe (o debería recibir) notificaciones cuando hay novedades.
- **Claridad:** Si no hay mensaje del tipo “Te asignaremos un técnico pronto” o “Un técnico ha sido asignado”, puede pensar que no pasa nada. Depende de los textos de la app y del correo.

## 6. Ver propuestas

- **Qué hace:** Quiere ver qué técnicos han ofertado y a qué precio.
- **Error funcional grave (bug H04):** En el sistema actual, cuando el **cliente** pide ver las propuestas de **su** solicitud, la aplicación le devuelve **lista vacía**. Solo el administrador ve todas las propuestas; el técnico ve las suyas. El dueño del trabajo no ve las ofertas que han hecho por su propio servicio.
- **Impacto emocional y funcional:** Juan cree que nadie ha ofertado o que la app está rota. No puede tomar ninguna decisión informada ni sentir que tiene control. Para alguien con urgencia, esto genera **desconfianza y frustración**.
- **Mensaje:** Si la pantalla dice “No hay propuestas” cuando sí las hay, el mensaje es **engañoso**. Si no dice nada y solo muestra vacío, es **confuso**.

## 7. Aceptar propuesta

- **Qué hace:** En muchos sistemas similares, el cliente “acepta” la oferta que prefiere.
- **Realidad en FixHub:** El cliente **no puede** aceptar. Solo un **administrador** puede asignar un técnico (aceptar una propuesta). Si la interfaz sugiere que el cliente va a elegir, es **inconsistente** con el diseño real. Si no lo explica, Juan no entiende por qué no puede hacer nada.
- **Claridad:** Debería explicarse en algún sitio: “Un asesor asignará el mejor técnico para tu caso” o similar.

## 8. Ver estado cambiar

- **Qué hace:** Ve que su solicitud pasa a “Asignado”, luego “En curso” cuando el técnico marca que ya empezó.
- **Claridad:** Si los estados tienen nombres claros (“Técnico asignado”, “El técnico está en camino”, “Trabajo en curso”), está bien. Si son “Assigned”, “InProgress”, no es humano.
- **Confianza:** Poder seguir el progreso reduce ansiedad.

## 9. Marcar completado

- **Qué hace:** Cuando el técnico termina, **Juan** es quien marca en la app que el servicio está completado (no el técnico).
- **Claridad:** Tiene sentido desde el punto de vista “el cliente confirma que quedó bien”. Debe estar muy visible el botón o la acción “Confirmar que el trabajo está terminado” o “Dar por cerrado”.
- **Posible error:** Si Juan intenta marcar completado antes de que el técnico haya “iniciado” el trabajo, el sistema lo rechaza. El mensaje debe ser claro (“Aún no puedes cerrar esta solicitud” / “El técnico debe indicar que inició el trabajo antes”) y no un código tipo INVALID_STATUS.

## 10. Calificar servicio

- **Qué hace:** Da estrellas (1 a 5) y opcionalmente un comentario.
- **Claridad:** Intuitivo si la pantalla es “¿Cómo fue el servicio?” con estrellas. Solo se puede calificar una vez por solicitud; si intenta de nuevo, el mensaje debe ser “Ya calificaste este servicio”, no un código técnico.
- **Confianza:** Poder opinar cierra bien la experiencia.

---

**Resumen cliente (Juan):** El fallo más grave es **no poder ver las propuestas** de su propia solicitud. Eso rompe la transparencia y la sensación de control. Además, debe quedar claro que quien asigna es la empresa, no él. El resto del flujo puede ser aceptable si los textos son humanos y los estados se entienden.

---

# FASE 3 — Prueba como técnico real (Carlos Gómez)

**Contexto:** Carlos Gómez. Técnico en refrigeración. Objetivo: conseguir trabajos y cobrar.

## 1. Registro como técnico

- **Qué hace:** Se registra y elige “Técnico” (o rol equivalente).
- **Claridad:** Si el formulario es el mismo que para clientes y solo cambia un desplegable “Tipo de cuenta”, está bien. Si pide “Role: 2”, no es humano.
- **Problema:** Tras registrarse, su cuenta de técnico queda en **“Pending”** (pendiente de aprobación). Si la app no le dice “Tu cuenta está en revisión; te avisaremos cuando puedas recibir trabajos”, Carlos puede creer que ya puede trabajar y no entender por qué no ve asignaciones.

## 2. Login

- **Qué hace:** Entra con email y contraseña.
- **Claridad:** Igual que para cliente. Sin comentarios si los mensajes son claros.

## 3. Ver trabajos disponibles

- **Qué hace:** Ve una lista de solicitudes abiertas (y las que él ya ha propuesto).
- **Claridad:** Debe entender “estos son trabajos a los que puedes postularte”. Si no hay ninguno, un mensaje “No hay trabajos disponibles ahora” es mejor que una lista vacía sin explicación.
- **Ambigüedad:** Si aún está Pending, no está claro si no ve trabajos porque no hay o porque no está aprobado. Debería verse un aviso de “Cuenta en revisión”.

## 4. Enviar propuesta

- **Qué hace:** Elige un trabajo, pone su precio y un mensaje, envía.
- **Claridad:** Intuitivo si hay un botón “Enviar mi propuesta” o “Ofertar”. Solo puede una propuesta por trabajo; si intenta otra, el mensaje debe ser “Ya enviaste una propuesta para este trabajo”, no DUPLICATE_PROPOSAL.
- **Seguridad:** Debe sentirse seguro de que su oferta llega y que no la puede cambiar por error (si no hay edición, está bien que quede claro).

## 5. Esperar aceptación

- **Qué hace:** Espera a que un administrador acepte su propuesta (o la de otro). No puede “aceptar” él mismo.
- **Claridad:** Si no se explica que “La empresa revisará las propuestas y asignará el trabajo”, puede parecer que el sistema está lento o roto. Una notificación “Has sido asignado al trabajo [X]” es clave.
- **Ambigüedad:** No sabe cuánto tarda la empresa en decidir; no hay plazos visibles.

## 6. Iniciar trabajo

- **Qué hace:** Cuando ya está asignado, marca “Iniciar trabajo” o “Empezar servicio” (el estado pasa a “En curso”).
- **Claridad:** Debe estar muy visible en “Mis asignaciones” o en el detalle del trabajo. Si intenta iniciar un trabajo que no es suyo o que aún no está asignado a él, el mensaje debe ser “No puedes iniciar este trabajo” (o similar), no FORBIDDEN.
- **Lógica:** Tiene sentido que solo el técnico asignado pueda marcar “en curso”.

## 7. Completar trabajo

- **Qué hace:** En la vida real, Carlos termina la reparación. En la app, **él no marca “completado”**. Quien cierra el trabajo es el **cliente** (o un admin).
- **Problema de expectativa:** Cualquier técnico esperaría “yo termino, yo marco listo”. Aquí no. Si la pantalla no lo dice explícitamente (“El cliente confirmará cuando el trabajo esté terminado”), Carlos puede buscar un botón que no existe y sentirse perdido.
- **Estado imposible desde su vista:** No hay acción “Completar” para el técnico. Si la interfaz sugiere lo contrario, hay inconsistencia.

---

**Resumen técnico (Carlos):** La lógica “quien cierra es el cliente” es válida, pero debe **comunicarse bien**. El estado “Pending” hasta aprobación debe ser **visible y explicado**. Ver trabajos disponibles y enviar propuestas puede ser fluido si los textos y mensajes de error son humanos.

---

# FASE 4 — Comportamientos humanos típicos

## Doble clic / Enviar dos veces

- **Completar dos veces:** Si el cliente hace doble clic en “Marcar como completado”, el sistema rechaza la segunda vez (ya está completado). Debe mostrarse un mensaje claro (“Este servicio ya está cerrado”) y no un error técnico.
- **Enviar propuesta dos veces:** El técnico no puede enviar dos propuestas al mismo trabajo; el sistema lo impide. El mensaje debe ser “Ya enviaste una propuesta para este trabajo”, no DUPLICATE_PROPOSAL.

## Formulario incompleto

- **Registro:** Si falta nombre, email o contraseña, debe indicarse qué campo falta y, si aplica, que la contraseña debe tener mayúscula, número y mínimo de caracteres, con mensajes **claros** (no solo “Validation failed”).
- **Crear solicitud:** Si falta título, descripción o dirección, debe decir qué falta. Si la categoría es inválida, “Categoría no válida” o “Selecciona un tipo de servicio” es mejor que CATEGORY_NOT_FOUND.

## Volver atrás / Refrescar

- **Comportamiento esperado:** Al volver atrás o refrescar, no debería perderse la sesión sin aviso. Si se pierde, un “Tu sesión expiró; inicia sesión de nuevo” es mejor que un 401 genérico.
- **Datos del formulario:** Si al volver atrás se pierde lo que escribió en un formulario largo, es fricción. No confirmado si la web guarda borrador.

## Repetir acción fuera de orden

- **Completar antes de que el técnico inicie:** El sistema lo rechaza (estado no es InProgress/Assigned). El mensaje debe ser comprensible: “Aún no puedes cerrar este servicio” o “El técnico debe indicar que inició el trabajo antes”.
- **Cancelar algo ya completado:** El sistema lo rechaza. Mensaje esperado: “No puedes cancelar un servicio ya terminado”.
- **Calificar dos veces:** Rechazado. “Solo puedes calificar una vez por servicio.”

Si en todos estos casos el usuario ve códigos (INVALID_STATUS, REVIEW_EXISTS, etc.) en lugar de frases, la experiencia es **mala** y parece un error del sistema.

---

# FASE 5 — Reporte final como QA humano

## 1) Experiencia cliente (1–10): **4/10**

- **Motivo:** No puede ver las propuestas de su propia solicitud (bug). No queda claro que quien asigna es la empresa. Si además los mensajes de error son técnicos y los estados en inglés, la sensación de control y transparencia es baja. El flujo básico (crear solicitud, ver estado, completar, calificar) puede funcionar si se corrige la vista de propuestas y se humanizan textos.

## 2) Experiencia técnico (1–10): **5/10**

- **Motivo:** El modelo “la empresa asigna, el cliente cierra” no es malo, pero debe explicarse. El estado Pending sin explicación genera confusión. No poder “completar” el trabajo desde su cuenta es lógico pero contraintuitivo si no se comunica. Ver trabajos y enviar propuestas puede ser aceptable.

## 3) Fluidez del flujo

- **Cliente:** Rompe en “ver propuestas”; el resto puede fluir si hay un admin que asigna y los textos guían.
- **Técnico:** Fluye si está aprobado y si entiende que no cierra él el trabajo. Falta guía en “qué sigue” después de enviar propuesta y después de iniciar.

## 4) Claridad de mensajes

- **Riesgo alto:** Si la aplicación muestra códigos de error (INVALID_STATUS, FORBIDDEN, DUPLICATE_PROPOSAL, CATEGORY_NOT_FOUND, REVIEW_EXISTS) al usuario final, no son humanos. Deben traducirse a frases (“No puedes hacer esto ahora”, “Ya enviaste una propuesta”, “Elige un tipo de servicio”, “Solo puedes calificar una vez”).
- **Estados:** “Open”, “Assigned”, “InProgress”, “Completed”, “Cancelled” deben ser en español y con significado claro para el usuario.

## 5) Errores encontrados

| Qué | Severidad | Impacto para la persona |
|-----|-----------|--------------------------|
| Cliente no ve propuestas de su solicitud | **Grave** | Cree que nadie ofertó o que la app falla; pierde confianza. |
| Registro permite crearse Admin | **Crítico (seguridad)** | Cualquiera puede tener permisos de empresa; no es algo que “vea” el usuario normal pero invalida la confianza en el sistema. |
| Mensajes de error técnicos en pantalla | **Alto** | El usuario no entiende qué pasó ni qué hacer. |
| Técnico en Pending sin explicación | **Alto** | “Me registré y no puedo hacer nada” sin saber por qué. |
| Cliente no puede “aceptar” propuesta (solo admin) | **Medio** | Confusión si la expectativa es “yo elijo”; debe explicarse en la UI. |
| Técnico no tiene botón “Completar” | **Medio** | Confusión si no se explica que el cliente cierra el trabajo. |

## 6) Riesgos funcionales

- Cliente abandona porque “no ve quién me va a atender”.
- Técnico abandona porque “nunca me aprobaron” o “no sé cuándo puedo marcar listo”.
- Doble clic o repetición de acción genera mensaje incomprensible y sensación de “la app se rompió”.
- Categorías o campos que piden ID/número en lugar de lista legible impiden a usuarios no técnicos completar el flujo.

## 7) Inconsistencias lógicas

- **“Ver propuestas”** existe para el cliente pero no le muestra las de su job (lista vacía). Lógicamente, si hay pantalla de propuestas, el dueño del trabajo debería verlas.
- **“Aceptar propuesta”** en cabeza del cliente no existe en el backend (solo admin). Si la interfaz sugiere que el cliente elige, es inconsistente.
- **Cerrar el trabajo** lo hace el cliente, no el técnico. Es una decisión de diseño válida pero debe ser la misma en toda la app y en los textos.

## 8) Mejoras urgentes

1. **Corregir la vista de propuestas para el cliente:** Que el dueño de la solicitud vea las propuestas de su trabajo. Sin esto, la experiencia cliente es deficiente.
2. **No permitir registrarse como Admin:** Solo la empresa debe crear admins. Es crítico de seguridad.
3. **Mensajes en lenguaje humano:** Sustituir códigos (INVALID_STATUS, FORBIDDEN, etc.) por frases claras según el contexto.
4. **Explicar al técnico:** “Tu cuenta está en revisión” mientras esté Pending; “El cliente confirmará cuando el trabajo esté terminado” para no buscar un botón “Completar”.
5. **Explicar al cliente:** “Un asesor asignará el mejor técnico para tu caso” para que no espere un botón “Aceptar propuesta”.
6. **Estados y etiquetas en español** y comprensibles (y coherentes en toda la app).
7. **Categorías/tipos de servicio** como lista o desplegable legible, no solo un número.

## 9) Nivel de preparación real

**NOT READY** para uso por personas reales en el estado actual.

**Motivos:**

- Un cliente que quiere ver quién le ha ofertado **no puede** (bug de propuestas). Eso solo ya justifica no dar el sistema por listo.
- Permitir que cualquiera se registre como administrador es **inadmisible** en producción.
- Si los mensajes que ve el usuario son técnicos, cada error se vive como “la app no funciona” en lugar de “entendí qué pasó”.

**Condiciones para READY:**

- Cliente ve las propuestas de su propia solicitud.
- Registro no permite rol Admin.
- Mensajes de error y estados en lenguaje claro para el usuario.
- Explicaciones mínimas para técnico (Pending, quién cierra el trabajo) y para cliente (quién asigna).
- Categorías/tipos de servicio seleccionables de forma clara en la interfaz.

---

*Reporte generado desde la perspectiva de usuario real (cliente Juan Pérez, técnico Carlos Gómez). Basado en el comportamiento documentado del sistema; no se inventan funcionalidades.*
