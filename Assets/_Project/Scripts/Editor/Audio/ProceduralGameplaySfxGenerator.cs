#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GanhHangRong.EditorTools
{
    public static class ProceduralGameplaySfxGenerator
    {
        private const int SampleRate = 44100;
        private const string OutputDirectory = "Assets/_Project/Audio/SFX/Resources/GHR_SFX";

        [MenuItem("Tools/Ganh Hang Rong/Audio/Regenerate Gameplay SFX")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(OutputDirectory);

            var clips = new Dictionary<string, float[]>
            {
                { "cup_pickup", Create(0.22f, 101, BuildCupPickup) },
                { "cup_place", Create(0.26f, 102, BuildCupPlace) },
                { "pour_water", Create(0.86f, 103, BuildPourWater) },
                { "add_ingredient", Create(0.34f, 104, BuildIngredient) },
                { "add_ice", Create(0.48f, 105, BuildIce) },
                { "stove_ignite", Create(0.58f, 106, BuildStoveIgnite) },
                { "boiling_loop", Create(2.0f, 107, BuildBoilingLoop, true) },
                { "kettle_ready", Create(0.76f, 108, BuildKettleReady) },
                { "wash_cup", Create(1.42f, 109, BuildWashCup) },
                { "drink_ready", Create(0.68f, 110, BuildDrinkReady) },
                { "serve_success", Create(0.62f, 111, BuildServeSuccess) },
                { "payment", Create(0.58f, 112, BuildPayment) },
                { "customer_arrive", Create(0.42f, 113, BuildCustomerArrive) },
                { "order_bell", Create(0.32f, 114, BuildOrderBell) },
                { "error", Create(0.34f, 115, BuildError) },
                { "shop_open", Create(0.78f, 116, BuildShopOpen) },
                { "shop_close", Create(0.78f, 117, BuildShopClose) },
                { "menu_open", Create(0.24f, 118, BuildMenuOpen) },
                { "menu_close", Create(0.2f, 119, BuildMenuClose) }
            };

            foreach (KeyValuePair<string, float[]> clip in clips)
            {
                string path = $"{OutputDirectory}/{clip.Key}.wav";
                WritePcm16Wave(path, clip.Value);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporters(clips.Keys);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Gameplay SFX] Generated {clips.Count} WAV files in {OutputDirectory}.");
        }

        private static float[] Create(float seconds, int seed, Action<float[], System.Random> builder, bool loop = false)
        {
            float[] data = new float[Mathf.CeilToInt(seconds * SampleRate)];
            builder(data, new System.Random(seed));
            Finish(data, loop ? 0.66f : 0.72f, loop);
            return data;
        }

        private static void BuildCupPickup(float[] data, System.Random random)
        {
            AddImpact(data, 0.012f, 0.58f, 920f, random);
            AddMode(data, 0.014f, 0.2f, 1540f, 0.27f, 19f);
            AddMode(data, 0.014f, 0.16f, 2380f, 0.14f, 25f);
            AddNoise(data, 0.006f, 0.018f, 0.12f, 0.32f, random);
        }

        private static void BuildCupPlace(float[] data, System.Random random)
        {
            AddImpact(data, 0.018f, 0.68f, 680f, random);
            AddImpact(data, 0.062f, 0.22f, 890f, random);
            AddMode(data, 0.02f, 0.22f, 1280f, 0.2f, 17f);
            AddNoise(data, 0.012f, 0.06f, 0.1f, 0.18f, random);
        }

        private static void BuildPourWater(float[] data, System.Random random)
        {
            AddBandNoise(data, 0.025f, 0.78f, 0.3f, 0.09f, 0.16f, random);
            AddMode(data, 0.06f, 0.7f, 164f, 0.035f, 2.6f);
            AddDroplet(data, 0.19f, 1250f, 0.16f);
            AddDroplet(data, 0.43f, 1680f, 0.13f);
            AddDroplet(data, 0.7f, 1120f, 0.11f);
        }

        private static void BuildIngredient(float[] data, System.Random random)
        {
            AddGranular(data, 0.018f, 0.23f, 38, 0.22f, random);
            AddBandNoise(data, 0.03f, 0.2f, 0.11f, 0.18f, 0.08f, random);
            AddImpact(data, 0.245f, 0.2f, 760f, random);
        }

        private static void BuildIce(float[] data, System.Random random)
        {
            float[] times = { 0.018f, 0.075f, 0.146f, 0.24f, 0.32f };
            for (int i = 0; i < times.Length; i++)
            {
                float baseFrequency = 1450f + (float)random.NextDouble() * 1500f;
                AddImpact(data, times[i], 0.46f - i * 0.055f, baseFrequency, random);
                AddMode(data, times[i], 0.12f, baseFrequency * 1.73f, 0.13f, 29f);
            }
        }

        private static void BuildStoveIgnite(float[] data, System.Random random)
        {
            AddImpact(data, 0.012f, 0.5f, 1180f, random);
            AddImpact(data, 0.072f, 0.38f, 980f, random);
            AddBandNoise(data, 0.09f, 0.45f, 0.24f, 0.045f, 0.2f, random);
            AddMode(data, 0.12f, 0.4f, 92f, 0.045f, 4f);
        }

        private static void BuildBoilingLoop(float[] data, System.Random random)
        {
            AddBandNoise(data, 0f, 2f, 0.18f, 0.035f, 0.12f, random, 0.045f, 0.045f);
            AddMode(data, 0f, 2f, 78f, 0.022f, 0.15f);

            for (int i = 0; i < 18; i++)
            {
                float time = 0.08f + (float)random.NextDouble() * 1.78f;
                float frequency = 260f + (float)random.NextDouble() * 620f;
                AddBubble(data, time, frequency, 0.05f + (float)random.NextDouble() * 0.07f);
            }
        }

        private static void BuildKettleReady(float[] data, System.Random random)
        {
            AddImpact(data, 0.012f, 0.38f, 720f, random);
            AddImpact(data, 0.085f, 0.24f, 940f, random);
            AddTone(data, 0.12f, 0.5f, 1320f, 0.18f, 0.012f, 0.28f);
            AddTone(data, 0.22f, 0.44f, 1760f, 0.13f, 0.01f, 0.22f);
            AddBandNoise(data, 0.08f, 0.5f, 0.07f, 0.06f, 0.18f, random);
        }

        private static void BuildWashCup(float[] data, System.Random random)
        {
            AddBandNoise(data, 0.02f, 1.28f, 0.26f, 0.08f, 0.16f, random, 0.05f, 0.15f);
            AddDroplet(data, 0.23f, 1080f, 0.1f);
            AddDroplet(data, 0.61f, 1380f, 0.11f);
            AddDroplet(data, 1.02f, 920f, 0.09f);
            AddImpact(data, 1.25f, 0.25f, 760f, random);
        }

        private static void BuildDrinkReady(float[] data, System.Random random)
        {
            AddImpact(data, 0.012f, 0.24f, 980f, random);
            AddTone(data, 0.055f, 0.34f, 659.25f, 0.28f, 0.008f, 0.22f);
            AddTone(data, 0.19f, 0.42f, 880f, 0.31f, 0.008f, 0.28f);
            AddMode(data, 0.19f, 0.42f, 1760f, 0.08f, 8f);
        }

        private static void BuildServeSuccess(float[] data, System.Random random)
        {
            AddImpact(data, 0.012f, 0.42f, 640f, random);
            AddMode(data, 0.016f, 0.22f, 1420f, 0.17f, 18f);
            AddTone(data, 0.12f, 0.38f, 740f, 0.22f, 0.008f, 0.23f);
            AddTone(data, 0.23f, 0.34f, 987.77f, 0.19f, 0.008f, 0.22f);
        }

        private static void BuildPayment(float[] data, System.Random random)
        {
            AddImpact(data, 0.012f, 0.38f, 1680f, random);
            AddMode(data, 0.012f, 0.24f, 2960f, 0.2f, 18f);
            AddImpact(data, 0.11f, 0.3f, 1920f, random);
            AddMode(data, 0.11f, 0.28f, 3420f, 0.16f, 15f);
            AddImpact(data, 0.22f, 0.2f, 1240f, random);
        }

        private static void BuildCustomerArrive(float[] data, System.Random random)
        {
            AddImpact(data, 0.008f, 0.32f, 520f, random);
            AddTone(data, 0.018f, 0.36f, 1046.5f, 0.27f, 0.006f, 0.25f);
            AddMode(data, 0.018f, 0.37f, 2093f, 0.11f, 9f);
        }

        private static void BuildOrderBell(float[] data, System.Random random)
        {
            AddImpact(data, 0.008f, 0.24f, 920f, random);
            AddTone(data, 0.014f, 0.28f, 1318.5f, 0.24f, 0.005f, 0.2f);
            AddMode(data, 0.014f, 0.28f, 2637f, 0.08f, 12f);
        }

        private static void BuildError(float[] data, System.Random random)
        {
            AddImpact(data, 0.012f, 0.5f, 190f, random);
            AddImpact(data, 0.13f, 0.38f, 158f, random);
            AddMode(data, 0.012f, 0.3f, 382f, 0.12f, 12f);
        }

        private static void BuildShopOpen(float[] data, System.Random random)
        {
            AddImpact(data, 0.012f, 0.58f, 260f, random);
            AddImpact(data, 0.075f, 0.34f, 430f, random);
            AddTone(data, 0.15f, 0.4f, 784f, 0.24f, 0.008f, 0.27f);
            AddTone(data, 0.29f, 0.4f, 1046.5f, 0.2f, 0.008f, 0.25f);
        }

        private static void BuildShopClose(float[] data, System.Random random)
        {
            AddImpact(data, 0.012f, 0.55f, 240f, random);
            AddImpact(data, 0.075f, 0.32f, 390f, random);
            AddTone(data, 0.15f, 0.4f, 880f, 0.22f, 0.008f, 0.25f);
            AddTone(data, 0.29f, 0.4f, 659.25f, 0.19f, 0.008f, 0.24f);
        }

        private static void BuildMenuOpen(float[] data, System.Random random)
        {
            AddBandNoise(data, 0.008f, 0.18f, 0.11f, 0.12f, 0.07f, random);
            AddTone(data, 0.025f, 0.18f, 720f, 0.14f, 0.008f, 0.12f);
            AddTone(data, 0.07f, 0.14f, 960f, 0.11f, 0.006f, 0.1f);
        }

        private static void BuildMenuClose(float[] data, System.Random random)
        {
            AddBandNoise(data, 0.006f, 0.15f, 0.1f, 0.1f, 0.08f, random);
            AddTone(data, 0.018f, 0.15f, 820f, 0.12f, 0.006f, 0.1f);
            AddTone(data, 0.058f, 0.12f, 580f, 0.1f, 0.006f, 0.08f);
        }

        private static void AddImpact(float[] data, float start, float amplitude, float baseFrequency, System.Random random)
        {
            AddNoise(data, start, 0.012f, amplitude * 0.55f, 0.4f, random);
            AddMode(data, start, 0.2f, baseFrequency, amplitude * 0.55f, 21f);
            AddMode(data, start, 0.16f, baseFrequency * 1.57f, amplitude * 0.27f, 27f);
            AddMode(data, start, 0.11f, baseFrequency * 2.31f, amplitude * 0.13f, 34f);
        }

        private static void AddDroplet(float[] data, float start, float frequency, float amplitude)
        {
            AddMode(data, start, 0.12f, frequency, amplitude, 31f);
            AddMode(data, start, 0.08f, frequency * 1.91f, amplitude * 0.34f, 42f);
        }

        private static void AddBubble(float[] data, float start, float frequency, float amplitude)
        {
            int startSample = Mathf.RoundToInt(start * SampleRate);
            int count = Mathf.RoundToInt(0.075f * SampleRate);
            for (int i = 0; i < count && startSample + i < data.Length; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Sin(Mathf.Clamp01(t / 0.018f) * Mathf.PI * 0.5f) * Mathf.Exp(-32f * t);
                float chirpedFrequency = frequency * (1f + t * 3.2f);
                data[startSample + i] += Mathf.Sin(2f * Mathf.PI * chirpedFrequency * t) * amplitude * envelope;
            }
        }

        private static void AddMode(float[] data, float start, float duration, float frequency, float amplitude, float decay)
        {
            int startSample = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
            int count = Mathf.RoundToInt(duration * SampleRate);
            for (int i = 0; i < count && startSample + i < data.Length; i++)
            {
                float t = i / (float)SampleRate;
                data[startSample + i] += Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * Mathf.Exp(-decay * t);
            }
        }

        private static void AddTone(float[] data, float start, float duration, float frequency, float amplitude,
            float attack, float release)
        {
            int startSample = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
            int count = Mathf.RoundToInt(duration * SampleRate);
            for (int i = 0; i < count && startSample + i < data.Length; i++)
            {
                float t = i / (float)SampleRate;
                float attackEnvelope = Mathf.Clamp01(t / Mathf.Max(0.001f, attack));
                float releaseEnvelope = Mathf.Clamp01((duration - t) / Mathf.Max(0.001f, release));
                float envelope = Smooth(attackEnvelope) * Smooth(releaseEnvelope);
                float fundamental = Mathf.Sin(2f * Mathf.PI * frequency * t);
                float harmonic = Mathf.Sin(2f * Mathf.PI * frequency * 2.01f * t) * 0.18f;
                data[startSample + i] += (fundamental + harmonic) * amplitude * envelope;
            }
        }

        private static void AddNoise(float[] data, float start, float duration, float amplitude, float lowPass,
            System.Random random)
        {
            int startSample = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
            int count = Mathf.RoundToInt(duration * SampleRate);
            float filtered = 0f;
            for (int i = 0; i < count && startSample + i < data.Length; i++)
            {
                float white = (float)random.NextDouble() * 2f - 1f;
                filtered += (white - filtered) * lowPass;
                float t = i / (float)Mathf.Max(1, count - 1);
                float envelope = Smooth(1f - t);
                data[startSample + i] += filtered * amplitude * envelope;
            }
        }

        private static void AddBandNoise(float[] data, float start, float duration, float amplitude, float slow,
            float fast, System.Random random, float fadeIn = 0.08f, float fadeOut = 0.12f)
        {
            int startSample = Mathf.Max(0, Mathf.RoundToInt(start * SampleRate));
            int count = Mathf.RoundToInt(duration * SampleRate);
            float low = 0f;
            float high = 0f;
            for (int i = 0; i < count && startSample + i < data.Length; i++)
            {
                float white = (float)random.NextDouble() * 2f - 1f;
                low += (white - low) * slow;
                high += (white - high) * fast;
                float t = i / (float)SampleRate;
                float envelope = Smooth(Mathf.Clamp01(t / fadeIn)) *
                    Smooth(Mathf.Clamp01((duration - t) / fadeOut));
                data[startSample + i] += (high - low) * amplitude * envelope;
            }
        }

        private static void AddGranular(float[] data, float start, float duration, int grainCount, float amplitude,
            System.Random random)
        {
            for (int grain = 0; grain < grainCount; grain++)
            {
                float grainStart = start + (float)random.NextDouble() * duration;
                float frequency = 700f + (float)random.NextDouble() * 2400f;
                float grainAmplitude = amplitude * (0.35f + (float)random.NextDouble() * 0.65f);
                AddMode(data, grainStart, 0.016f, frequency, grainAmplitude, 120f);
            }
        }

        private static void Finish(float[] data, float targetPeak, bool loop)
        {
            float mean = 0f;
            for (int i = 0; i < data.Length; i++) mean += data[i];
            mean /= Mathf.Max(1, data.Length);

            float peak = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (float)Math.Tanh((data[i] - mean) * 1.15f);
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }

            float gain = peak > 0.0001f ? targetPeak / peak : 1f;
            int edgeSamples = Mathf.Min(Mathf.RoundToInt((loop ? 0.025f : 0.004f) * SampleRate), data.Length / 2);
            for (int i = 0; i < data.Length; i++)
            {
                float edge = 1f;
                if (i < edgeSamples) edge *= Smooth(i / (float)edgeSamples);
                if (i >= data.Length - edgeSamples) edge *= Smooth((data.Length - 1 - i) / (float)edgeSamples);
                data[i] = Mathf.Clamp(data[i] * gain * edge, -0.98f, 0.98f);
            }
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static void WritePcm16Wave(string path, float[] samples)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                const short channels = 1;
                const short bitsPerSample = 16;
                int dataSize = samples.Length * sizeof(short);

                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataSize);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write(channels);
                writer.Write(SampleRate);
                writer.Write(SampleRate * channels * bitsPerSample / 8);
                writer.Write((short)(channels * bitsPerSample / 8));
                writer.Write(bitsPerSample);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataSize);

                for (int i = 0; i < samples.Length; i++)
                {
                    writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue));
                }
            }
        }

        private static void ConfigureImporters(IEnumerable<string> clipNames)
        {
            foreach (string clipName in clipNames)
            {
                string path = $"{OutputDirectory}/{clipName}.wav";
                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null) continue;

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                settings.preloadAudioData = true;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.loadInBackground = false;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
