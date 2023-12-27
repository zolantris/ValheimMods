namespace ValheimRAFT
{
    public interface Interactable
    {
        bool Interact(Humanoid user, bool hold, bool alt);

        bool UseItem(Humanoid user, ItemDrop.ItemData item);
    }
}
