using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes;

namespace BlackSouls.Scripts;

/// <summary>Centralizes BS Ancient audio resource paths and playback helpers.</summary>
public static class BsAncientAudio
{
    public const string Clock = "res://bs_ancient/assets/audio/clock.mp3";
    public const string Write = "res://bs_ancient/assets/audio/write.mp3";
    public const string Cat = "res://bs_ancient/assets/audio/cat.mp3";
    public const string Shot = "res://bs_ancient/assets/audio/shot.mp3";
    public const string Claps = "res://bs_ancient/assets/audio/claps.wav";
    public const string Boos = "res://bs_ancient/assets/audio/hiss.mp3";
    public const string StageEnd = "res://bs_ancient/assets/audio/stageend.mp3";

    private static AudioStreamPlayer? _stageEndPlayer;

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

    public static void PlayStageEndLoop(float volume = 1f)
    {
        if (NonInteractiveMode.IsActive || NGame.Instance == null)
        {
            return;
        }

        if (GodotObject.IsInstanceValid(_stageEndPlayer) && _stageEndPlayer!.Playing)
        {
            return;
        }

        StopStageEndLoop();

        AudioStream? stream = ResourceLoader.Load<AudioStream>(StageEnd);
        if (stream == null)
        {
            return;
        }

        if (stream is AudioStreamMP3 mp3)
        {
            mp3.Loop = true;
        }

        _stageEndPlayer = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeDb = Mathf.LinearToDb(Mathf.Max(0.001f, volume))
        };

        NGame.Instance.AddChild(_stageEndPlayer);
        _stageEndPlayer.Play();
    }

    public static void StopStageEndLoop()
    {
        if (!GodotObject.IsInstanceValid(_stageEndPlayer))
        {
            _stageEndPlayer = null;
            return;
        }

        _stageEndPlayer!.Stop();
        _stageEndPlayer.QueueFree();
        _stageEndPlayer = null;
    }
}
