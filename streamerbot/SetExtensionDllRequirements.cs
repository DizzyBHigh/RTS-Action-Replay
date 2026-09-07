// Streamer.bot C# action: define the RtsUI.dll requirements for the next action.
// The generic RTS - UI DLL Check action reads these arguments.
public class CPHInline
{
    public bool Execute()
    {
        CPH.SetArgument("rts.extensionName", "RTS Action Replay");
        CPH.SetArgument("rts.minimumRtsUiVersion", "0.2.0");
        return true;
    }
}
