using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;

public class CPHInline
{
    public bool Execute()
    {
        ApplyPlayerSettings();
        string command = CPH.GetArg<string>("replayCommand") ?? "load";
        CPH.SetArgument("replayCommand", command);
        CPH.TriggerEvent("RTS-Action Replay", true);
        return true;
    }

    private void ApplyPlayerSettings()
    {
        CPH.SetArgument("replayShowBranding", CPH.GetGlobalVar<bool?>("rts.actionreplay.showPlayerBranding", true) ?? true);
        CPH.SetArgument("replayLogoUrl", CPH.GetGlobalVar<string>("rts.actionreplay.brandLogoUrl", true) ?? "");
        CPH.SetArgument("replayShowTitle", CPH.GetGlobalVar<bool?>("rts.actionreplay.showPlayerTitle", true) ?? true);
        CPH.SetArgument("replayTitle", CPH.GetGlobalVar<string>("rts.actionreplay.playerTitle", true) ?? "Replay");
        CPH.SetArgument("replayPlayerFont", CPH.GetGlobalVar<string>("rts.actionreplay.playerFont", true) ?? "Inter");
        CPH.SetArgument("replayShowControls", CPH.GetGlobalVar<bool?>("rts.actionreplay.showControls", true) ?? false);
        CPH.SetArgument("replayShowProgress", CPH.GetGlobalVar<bool?>("rts.actionreplay.showProgress", true) ?? false);
        CPH.SetArgument("replayFrameColor", CPH.GetGlobalVar<string>("rts.actionreplay.frameColor", true) ?? "#0384CB");
        CPH.SetArgument("replayBorderColor", CPH.GetGlobalVar<string>("rts.actionreplay.borderColor", true) ?? "#FFFFFF");
        CPH.SetArgument("replayBorderWidth", CPH.GetGlobalVar<int?>("rts.actionreplay.borderWidth", true) ?? 2);
        CPH.SetArgument("replayBorderStyle", CPH.GetGlobalVar<string>("rts.actionreplay.borderStyle", true) ?? "Solid");
        CPH.SetArgument("replayDropShadow", CPH.GetGlobalVar<bool?>("rts.actionreplay.dropShadow", true) ?? false);
        CPH.SetArgument("replayShadowColor", CPH.GetGlobalVar<string>("rts.actionreplay.shadowColor", true) ?? "#80000000");
        string elementsJson = CPH.GetGlobalVar<string>("rts.actionreplay.playerElements", true) ?? DefaultElements();
        CPH.SetArgument("replayPlayerElements", elementsJson);
        SetElementArguments(elementsJson);
    }

    private void SetElementArguments(string json)
    {
        try
        {
            Dictionary<string, Element> elements = new JavaScriptSerializer().Deserialize<Dictionary<string, Element>>(json ?? DefaultElements());
            SetElement("Brand", elements);
            SetElement("Title", elements);
            SetElement("Play Speed Indicator", elements);
        }
        catch
        {
            SetArgument("Brand", new Element()); SetArgument("Title", new Element()); SetArgument("Play Speed Indicator", new Element());
        }
    }

    private void SetElement(string name, Dictionary<string, Element> elements)
    {
        Element element;
        if (!elements.TryGetValue(name, out element) || element == null) element = new Element();
        SetArgument(name, element);
    }

    private void SetArgument(string name, Element element)
    {
        string prefix = name == "Play Speed Indicator" ? "Speed" : name;
        CPH.SetArgument("replay" + prefix + "Scale", element.scale);
        CPH.SetArgument("replay" + prefix + "X", element.x);
        CPH.SetArgument("replay" + prefix + "Y", element.y);
    }

    private static string DefaultElements()
    {
        return "{\"Brand\":{\"name\":\"Brand\",\"scale\":100,\"x\":0,\"y\":0},\"Title\":{\"name\":\"Title\",\"scale\":100,\"x\":0,\"y\":0},\"Play Speed Indicator\":{\"name\":\"Play Speed Indicator\",\"scale\":100,\"x\":0,\"y\":0}}";
    }

    private sealed class Element
    {
        public int scale = 100;
        public int x;
        public int y;
    }
}
