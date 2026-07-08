using Godot;

namespace BlackSouls.Scripts.Nodes.Events;

/// <summary>Displays fullscreen event background art.</summary>
public partial class BsAncientFullscreenBackground : Control
{
    public override void _Ready()
    {
        ResetParentTransform();
    }

    public override void _Process(double delta)
    {
        ResetParentTransform();
    }

    private void ResetParentTransform()
    {
        if (GetParent() is not Control parent)
        {
            return;
        }

        parent.Position = Vector2.Zero;
        parent.Scale = Vector2.One;
        parent.PivotOffset = Vector2.Zero;
    }
}
