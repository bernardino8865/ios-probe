using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IosProbe;

/// <summary>
/// Teste de fumaça do que o Mini Campus faz no boot: JSON por source generator, leitura de res:// com FileAccess,
/// gravação em user://, thread própria e textura criada de dados já comprimidos (ASTC). Mostra o resultado na tela
/// e no log ("[PROBE] RESULT: PASS" ou "FAIL").
/// </summary>
public partial class Main : Control
{
    public override void _Ready()
    {
        var lines = new List<string>();
        var allPassed = true;

        void Check(string name, bool passed, string detail = "")
        {
            allPassed &= passed;
            var line = $"[{(passed ? "OK" : "FALHOU")}] {name}" + (detail.Length > 0 ? $" - {detail}" : "");
            lines.Add(line);
            GD.Print("[PROBE] " + line);
        }

        // 1. JSON por source generator, lido de res:// com FileAccess (o jeito certo no export).
        try
        {
            var text = Godot.FileAccess.GetFileAsString("res://data/probe.json");
            var data = JsonSerializer.Deserialize(text, ProbeJson.Default.ProbeData);
            Check("JSON source generator + FileAccess(res://)", data is { Name: "mini-campus-probe", Numbers.Length: 3 }, data?.Name ?? "nulo");
        }
        catch (Exception e) { Check("JSON source generator + FileAccess(res://)", false, e.GetType().Name + ": " + e.Message); }

        // 2. Gravar e ler em user:// e serializar de volta (como o roster e o perfil do jogador).
        try
        {
            const string path = "user://probe_roundtrip.json";
            using (var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write))
                file.StoreString(JsonSerializer.Serialize(new ProbeData { Name = "volta", Numbers = [7, 8] }, ProbeJson.Default.ProbeData));
            var back = JsonSerializer.Deserialize(Godot.FileAccess.GetFileAsString(path), ProbeJson.Default.ProbeData);
            Check("Gravar/ler user:// (Serialize + Deserialize)", back is { Name: "volta", Numbers.Length: 2 });
        }
        catch (Exception e) { Check("Gravar/ler user:// (Serialize + Deserialize)", false, e.GetType().Name + ": " + e.Message); }

        // 3. Thread própria (o jogo monta pacotes numa thread de trabalho).
        try
        {
            var value = 0;
            var thread = new System.Threading.Thread(() => value = 42);
            thread.Start();
            thread.Join();
            Check("Thread de trabalho", value == 42);
        }
        catch (Exception e) { Check("Thread de trabalho", false, e.GetType().Name + ": " + e.Message); }

        // 4. Carregar um recurso síncrono do pacote (ResourceLoader.Load, sem LoadThreaded: trava em NativeAOT).
        try
        {
            var icon = ResourceLoader.Load<Texture2D>("res://icon.svg");
            Check("ResourceLoader.Load síncrono", icon is not null);
        }
        catch (Exception e) { Check("ResourceLoader.Load síncrono", false, e.GetType().Name + ": " + e.Message); }

        // 5. Textura criada direto de dados comprimidos ASTC 4x4 (base do plano de arquivo próprio por ator).
        //    8x8 pixels = 4 blocos de 16 bytes. Só informativo no PC (a placa de vídeo do PC não lê ASTC).
        try
        {
            var image = Image.CreateFromData(8, 8, false, Image.Format.Astc4X4, new byte[4 * 16]);
            var texture = ImageTexture.CreateFromImage(image);
            Check("Image.CreateFromData ASTC 4x4 (informativo no PC)", texture is not null && image.IsCompressed());
        }
        catch (Exception e) { Check("Image.CreateFromData ASTC 4x4 (informativo no PC)", false, e.GetType().Name + ": " + e.Message); }

        GD.Print("[PROBE] RESULT: " + (allPassed ? "PASS" : "FAIL"));

        var label = new Label
        {
            Text = "IosProbe  " + (allPassed ? "PASS" : "FAIL") + "\n\n" + string.Join("\n", lines),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        label.AddThemeFontSizeOverride("font_size", 22);
        label.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 24);
        AddChild(label);
    }
}

public sealed class ProbeData
{
    public string Name { get; set; } = "";
    public int[] Numbers { get; set; } = [];
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ProbeData))]
internal sealed partial class ProbeJson : JsonSerializerContext;
