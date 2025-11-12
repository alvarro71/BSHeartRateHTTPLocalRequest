# 🫀 BSHeartRateHTTPLocalRequest

Un **plugin para Beat Saber** que muestra tu ritmo cardíaco (BPM) directamente dentro del juego en texto 3D, utilizando datos locales.  
El plugin lee tu frecuencia cardíaca desde un archivo en tu PC y actualiza la visualización en tiempo real mientras juegas.

---

## ✨ Características

- Muestra el **BPM actual** en pantalla con texto 3D configurable.  
- Colores dinámicos y animaciones según el nivel de pulsaciones.  
- Configuración editable desde un archivo `.txt`.  
- Si superas el BPM máximo configurado, el juego se cerrará automáticamente (ideal para retos o vídeos).  
- Compatible con fuentes locales.

---

## ⚙️ Cómo funciona

El plugin carga la configuración desde un archivo en:

```
C:\xampp\htdocs\pulsometroalvarro71\Texto3DConfig.txt
```

Ejemplo de configuración:

```
# Configuración del plugin Texto3D
texto3d_enabled=1
max_pulsometer=180
text_size=0.02
text_pos_x=0
text_pos_y=1.5
text_pos_z=2.5
text_rot_x=0
text_rot_y=0
text_rot_z=0
bpm_path=C:\xampp\htdocs\pulsometroalvarro71\bpm.txt
```

El archivo `bpm.txt` debe contener únicamente un número, que representa tu frecuencia cardíaca actual (BPM).  
**Nota:** Este archivo se genera y actualiza a través del proyecto [Pulsometro Local](https://github.com/alvarro71/Pulsometro-Local).

---

## 🧩 Instalación

1. Instala **BSIPA** (Beat Saber IPA Plugin Loader).  
2. Descarga o compila el archivo `BSHeartRateHTTPLocalRequest.dll`.  
3. Colócalo en la carpeta:
   ```
   Beat Saber\Plugins\
   ```
4. Asegúrate de tener configurado correctamente el archivo `Texto3DConfig.txt` y el archivo `bpm.txt`.  
5. Inicia Beat Saber y verás tu ritmo cardíaco en pantalla. ❤️

---

## 🧠 Nota

Este plugin **no realiza peticiones HTTP externas** ni requiere conexión a internet.  
Todo se ejecuta de forma **local**, pensado para integraciones personalizadas o setups caseros.

---

## 📜 Licencia

Este proyecto está bajo la **MIT License**.  
Puedes usarlo, modificarlo y distribuirlo libremente, siempre dando crédito al autor original.

---

## 👨‍💻 Autor

Desarrollado por **Alvarro71**  
Este documento y el script incluyen pequeñas partes generadas con la ayuda de ChatGPT y GitHub Copilot.
