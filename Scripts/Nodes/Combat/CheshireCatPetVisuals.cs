using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BlackSouls.Scripts;

public partial class CheshireCatPetVisuals : NCreatureVisuals
{
    private const string CatAnimation = "animation";
    private const float SmileFadedAlpha = 0.35f;
    private const double SmileFadeDuration = 0.8;

    private Node2D? _smileVisuals;
    private Node2D? _biteVisuals;
    private MegaSprite? _smileSprite;
    private MegaSprite? _biteSprite;

    public override void _Ready()
    {
        base._Ready();

        _smileVisuals = GetNodeOrNull<Node2D>("%SmileVisuals");
        _biteVisuals = GetNodeOrNull<Node2D>("%BiteVisuals");
        _smileSprite = CreateSprite(_smileVisuals);
        _biteSprite = CreateSprite(_biteVisuals);

        ShowSmileIdle();
    }

    public void ShowSmileIdle()
    {
        if (_biteVisuals != null)
        {
            _biteVisuals.Visible = false;
        }

        if (_smileVisuals == null)
        {
            return;
        }

        _smileVisuals.Visible = true;
        _smileVisuals.Modulate = Colors.White;
        _smileSprite?.GetAnimationState().SetAnimation(CatAnimation, loop: true);
    }

    public void PlaySmile()
    {
        if (_biteVisuals != null)
        {
            _biteVisuals.Visible = false;
        }

        if (_smileVisuals == null)
        {
            return;
        }

        _smileVisuals.Visible = true;
        _smileVisuals.Modulate = Colors.White;
        _smileSprite?.GetAnimationState().SetAnimation(CatAnimation, loop: false);
        _smileSprite?.GetAnimationState().AddAnimation(CatAnimation, loop: true);
        _smileVisuals.CreateTween().TweenProperty(_smileVisuals, "modulate:a", SmileFadedAlpha, SmileFadeDuration);
    }

    public void PlayBite()
    {
        if (_smileVisuals != null)
        {
            _smileVisuals.Visible = false;
        }

        if (_biteVisuals == null)
        {
            return;
        }

        _biteVisuals.Visible = true;
        _biteVisuals.Modulate = Colors.White;
        _biteSprite?.GetAnimationState().SetAnimation(CatAnimation, loop: false);
    }

    private static MegaSprite? CreateSprite(Node2D? node)
    {
        if (node == null || node.GetClass() != "SpineSprite")
        {
            return null;
        }

        try
        {
            MegaSprite sprite = new(node);
            return sprite.GetSkeleton()?.GetData() == null ? null : sprite;
        }
        catch
        {
            return null;
        }
    }
}
