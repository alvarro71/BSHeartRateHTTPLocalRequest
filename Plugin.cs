using IPA;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BSHeartRateHTTPLocalRequest
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class BSHeartRateHTTPLocalRequest
    {
        public static IPA.Logging.Logger Log;

        [Init]
        public void Init(IPA.Logging.Logger logger)
        {
            Log = logger;
            Log.Info("<color=green>[Plugin]</color> Inicializado.");
        }

        [OnStart]
        public void OnApplicationStart()
        {
            PluginConfig.LoadConfig();
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnSceneChanged(Scene previous, Scene next)
        {
            if (PluginConfig.Texto3DEnabled)
                Texto3D.Create();
        }
    }

    public static class PluginConfig
    {
        private static string configPath = @"C:\xampp\htdocs\pulsometroalvarro71\Texto3DConfig.txt";

        public static bool Texto3DEnabled { get; private set; } = true;
        public static int MaxPulsometer { get; private set; } = 180;
        public static string PulsometerRaw { get; private set; } = "NULL";

        public static float TextSize = 0.02f;
        public static Vector3 TextPosition = new Vector3(0f, 1.5f, 2.5f);
        public static Vector3 TextRotation = Vector3.zero;

        public static string BpmPath = "";
        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    foreach (var line in File.ReadAllLines(configPath))
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("#") || string.IsNullOrEmpty(trimmed)) continue;
                        var parts = trimmed.Split(new char[] { '=' }, 2);
                        if (parts.Length != 2) continue;

                        var key = parts[0].Trim().ToLower();
                        var value = parts[1].Trim();

                        if (key == "texto3d_enabled") Texto3DEnabled = value == "1" || value.ToLower() == "true";
                        if (key == "max_pulsometer" && int.TryParse(value, out int tempInt)) MaxPulsometer = tempInt;
                        if (key == "text_size" && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float tempFloat)) TextSize = tempFloat;
                        if (key == "text_pos_x" && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tempFloat)) TextPosition.x = tempFloat;
                        if (key == "text_pos_y" && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tempFloat)) TextPosition.y = tempFloat;
                        if (key == "text_pos_z" && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tempFloat)) TextPosition.z = tempFloat;
                        if (key == "text_rot_x" && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tempFloat)) TextRotation.x = tempFloat;
                        if (key == "text_rot_y" && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tempFloat)) TextRotation.y = tempFloat;
                        if (key == "text_rot_z" && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tempFloat)) TextRotation.z = tempFloat;
                        if (key == "bpm_path") BpmPath = value;
                    }
                }
                else
                {
                    SaveConfig();
                }
            }
            catch (System.Exception ex)
            {
                BSHeartRateHTTPLocalRequest.Log?.Warn("[BPM Local] Error leyendo config: " + ex);
            }
        }

        public static void LoadBpm()
        {
            try
            {
                if (!File.Exists(BpmPath))
                {
                    PulsometerRaw = "NULL";
                    BSHeartRateHTTPLocalRequest.Log?.Info("[Pulsometer] Archivo no encontrado: " + BpmPath);
                    return;
                }

                string content = File.ReadAllText(BpmPath).Trim();
                if (int.TryParse(content, out int bpmValue))
                    PulsometerRaw = bpmValue.ToString();
                else
                    PulsometerRaw = "NULL";

                BSHeartRateHTTPLocalRequest.Log?.Info($"[BPM Local] BPM leído desde archivo: {PulsometerRaw}");
            }
            catch (Exception ex)
            {
                PulsometerRaw = "NULL";
                BSHeartRateHTTPLocalRequest.Log?.Warn("[BPM Local] Error leyendo BPM local: " + ex);
            }
        }

        public static void SaveConfig()
        {
            try
            {
                var lines = new string[]
                {
                    "# Configuración del plugin Texto3D",
                    "texto3d_enabled=" + (Texto3DEnabled ? "1" : "0"),
                    "max_pulsometer=" + MaxPulsometer,
                    "text_size=" + TextSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "text_pos_x=" + TextPosition.x.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "text_pos_y=" + TextPosition.y.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "text_pos_z=" + TextPosition.z.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "text_rot_x=" + TextRotation.x.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "text_rot_y=" + TextRotation.y.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "text_rot_z=" + TextRotation.z.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "bpm_path=" + BpmPath
                };
                File.WriteAllLines(configPath, lines);
            }
            catch (Exception ex)
            {
                BSHeartRateHTTPLocalRequest.Log?.Warn("Error guardando config: " + ex);
            }
        }
    }

    public class Texto3D : MonoBehaviour
    {
        public static Texto3D Instance { get; private set; }
        public TextMesh textMesh;
        public TextMesh sourceIndicatorMesh;
        public TextMesh overMaxCounterMesh;
        private float overMaxTimer = 0f;
        private float waitSeconds = 10f;
        private bool isReturningToMenu = false;
        private int currentBpm = 0;
        private float pulseTimer = 0f;
        private string lastSceneName = "";

        public static void Create()
        {
            if (Instance != null) return;
            var go = new GameObject("Texto3D_Global");
            go.AddComponent<Texto3D>();
            DontDestroyOnLoad(go);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Font customFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.font = customFont;
            textMesh.fontSize = 120;
            textMesh.characterSize = PluginConfig.TextSize;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.green;

            var meshRenderer = textMesh.GetComponent<MeshRenderer>();
            meshRenderer.material = textMesh.font.material;
            meshRenderer.sortingOrder = 9999;

            transform.position = PluginConfig.TextPosition;
            transform.rotation = Quaternion.Euler(PluginConfig.TextRotation);

            var sourceGO = new GameObject("SourceIndicator");
            sourceIndicatorMesh = sourceGO.AddComponent<TextMesh>();
            sourceIndicatorMesh.font = customFont;
            sourceIndicatorMesh.fontSize = 60;
            sourceIndicatorMesh.characterSize = PluginConfig.TextSize * 0.7f;
            sourceIndicatorMesh.anchor = TextAnchor.MiddleCenter;
            sourceIndicatorMesh.alignment = TextAlignment.Center;
            sourceIndicatorMesh.color = Color.gray;
            sourceGO.transform.parent = transform;
            sourceGO.transform.localPosition = new Vector3(0f, -0.5f, 0f);

            var overMaxRoot = new GameObject("OverMaxRoot");
            overMaxRoot.transform.position = transform.position; // mismo que el BPM
            overMaxRoot.transform.rotation = transform.rotation; // misma rotación
            DontDestroyOnLoad(overMaxRoot);

            var counterGO = new GameObject("OverMaxCounter");
            overMaxCounterMesh = counterGO.AddComponent<TextMesh>();
            overMaxCounterMesh.font = customFont;
            overMaxCounterMesh.fontSize = 80;
            overMaxCounterMesh.characterSize = PluginConfig.TextSize * 0.8f;
            overMaxCounterMesh.anchor = TextAnchor.MiddleCenter;
            overMaxCounterMesh.alignment = TextAlignment.Center;
            overMaxCounterMesh.color = Color.red;

            // Lo hacemos hijo de OverMaxRoot, pero su escala local será independiente
            counterGO.transform.parent = overMaxRoot.transform;
            counterGO.transform.localPosition = new Vector3(0f, 0.6f, 0f); // encima del BPM
            counterGO.transform.localRotation = Quaternion.identity;
            counterGO.transform.localScale = Vector3.one; // escala inicial

            StartCoroutine(BpmReaderLoop());
            StartCoroutine(PulseAnimationLoop());
        }


        private IEnumerator BpmReaderLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.5f);

                lastSceneName = SceneManager.GetActiveScene().name;

                PluginConfig.LoadConfig();
                PluginConfig.LoadBpm();

                if (int.TryParse(PluginConfig.PulsometerRaw, out int bpm))
                {
                    currentBpm = bpm;
                    textMesh.text = $"BPM: {bpm}";
                    textMesh.characterSize = PluginConfig.TextSize;
                }
            }
        }

        private Vector3 originalScale = Vector3.one;
        private Vector3 targetScale = Vector3.one;
        private Vector3 scaleVelocity = Vector3.zero;

        private void Update()
        {
            if (overMaxCounterMesh != null && overMaxCounterMesh.transform.parent != null)
            {
                overMaxCounterMesh.transform.parent.position = transform.position;
                overMaxCounterMesh.transform.parent.rotation = transform.rotation;
            }

            bool isGameplay = lastSceneName.ToLower().Contains("gamecore");
            if (!isGameplay || currentBpm <= 0)
            {
                overMaxTimer = 0f;
                if (overMaxCounterMesh != null)
                    overMaxCounterMesh.text = "";
                return;
            }

            if (currentBpm > PluginConfig.MaxPulsometer)
            {
                overMaxTimer += Time.deltaTime;
                int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(waitSeconds - overMaxTimer));

                float t = overMaxTimer / waitSeconds;
                float size = Mathf.Lerp(1f, 2.2f, t);

                targetScale = Vector3.one * size;

                float bounce = Mathf.Sin(Time.time * 8f) * 0.15f;
                float alpha = 0.5f + Mathf.PingPong(Time.time * 3f, 0.5f);

                Color epicColor = Color.red;
                epicColor.a = alpha;

                overMaxCounterMesh.transform.localPosition = new Vector3(0f, 0.6f + bounce, 0f);
                overMaxCounterMesh.transform.localScale = Vector3.SmoothDamp(
                    overMaxCounterMesh.transform.localScale,
                    targetScale,
                    ref scaleVelocity,
                    0.1f
                );
                overMaxCounterMesh.color = epicColor;
                overMaxCounterMesh.text = secondsLeft.ToString();

                if (overMaxTimer >= waitSeconds && !isReturningToMenu)
                {
                    isReturningToMenu = true;
                    ReturnToMenu();
                }
            }
            else
            {
                overMaxTimer = 0f;
                if (overMaxCounterMesh != null)
                {
                    overMaxCounterMesh.text = "";
                    overMaxCounterMesh.transform.localScale = Vector3.SmoothDamp(
                        overMaxCounterMesh.transform.localScale,
                        originalScale,
                        ref scaleVelocity,
                        0.1f
                    );
                }
            }
        }

        private void HandleOverMax(float delta)
        {
            overMaxTimer += delta;
            int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(waitSeconds - overMaxTimer));

            if (overMaxCounterMesh != null)
            {
                float bounce = Mathf.Sin(Time.time * 8f) * 0.15f;
                float counterScale = 1f + Mathf.PingPong(Time.time * 2f, 0.3f);
                float alpha = 0.5f + Mathf.PingPong(Time.time * 3f, 0.5f);

                Color epicColor = Color.red;
                epicColor.a = alpha;

                overMaxCounterMesh.transform.localPosition = new Vector3(0f, 0.6f + bounce, 0f);
                overMaxCounterMesh.transform.localScale = new Vector3(counterScale, counterScale, counterScale);
                overMaxCounterMesh.color = epicColor;
                overMaxCounterMesh.text = secondsLeft.ToString();
            }

            if (overMaxTimer >= waitSeconds && !isReturningToMenu)
            {
                isReturningToMenu = true;
                ReturnToMenu();
            }
        }

        private void ResetOverMax()
        {
            overMaxTimer = 0f;

            if (overMaxCounterMesh != null)
                overMaxCounterMesh.text = "";
        }

        private IEnumerator PulseAnimationLoop()
        {
            while (true)
            {
                if (currentBpm > 0)
                {
                    float beatsPerSec = currentBpm / 60f;
                    pulseTimer += Time.deltaTime * beatsPerSec * 2f;

                    float pulse = Mathf.Sin(pulseTimer * Mathf.PI);

                    float baseScale = 1f;
                    if (currentBpm >= 200) baseScale = 1.6f;
                    else if (currentBpm >= 170) baseScale = 1.4f;
                    else if (currentBpm >= 140) baseScale = 1.3f;
                    else if (currentBpm >= 120) baseScale = 1.2f;
                    else if (currentBpm >= 100) baseScale = 1.1f;

                    float finalScale = baseScale + pulse * 0.15f;

                    textMesh.transform.localScale = new Vector3(finalScale, finalScale, finalScale);
                }

                yield return null;
            }
        }

        private static void ReturnToMenu()
        {
            BSHeartRateHTTPLocalRequest.Log?.Info("[Texto3D] Limite superado, cerrando el juego...");
            if (Texto3D.Instance != null)
            {
                Texto3D.Instance.textMesh.color = Color.red;
                if (Texto3D.Instance.overMaxCounterMesh != null)
                    Texto3D.Instance.overMaxCounterMesh.text = "¡ADIOS!";
                Texto3D.Instance.StopAllCoroutines();
            }
            Application.Quit();
        }
    }
}