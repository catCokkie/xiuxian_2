using Godot;
using Xiuxian.Scripts.Contracts;

namespace Xiuxian.Scripts.Adapters.Godot
{
    public sealed class GodotConfigSource : IConfigSource
    {
        public bool TryReadAllText(string path, out string text)
        {
            using FileAccess? file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                text = string.Empty;
                return false;
            }

            text = file.GetAsText();
            return true;
        }
    }
}
