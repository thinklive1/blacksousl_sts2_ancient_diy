using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;

namespace BlackSouls.Scripts;

public static class BsAncientAudio
{
    public const string Clock = "res://bs_ancient/assets/audio/clock.mp3";
    public const string Write = "res://bs_ancient/assets/audio/write.mp3";

    public static void PlayOneShot(string path, float volume = 1f)
    {
        if (NonInteractiveMode.IsActive || NGame.Instance == null)
        {
            return;
        }

        AudioStream? stream = ResourceLoader.Load<AudioStream>(path);
        if (stream == null)
        {
            return;
        }

        AudioStreamPlayer player = new()
        {
            Stream = stream,
            VolumeDb = Mathf.LinearToDb(Mathf.Max(0.001f, volume))
        };

        player.Finished += player.QueueFree;
        NGame.Instance.AddChild(player);
        player.Play();
    }
}
