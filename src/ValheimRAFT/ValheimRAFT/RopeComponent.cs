using UnityEngine;

namespace ValheimRAFT;

public class RopeComponent : MonoBehaviour
{
	public SpringJoint m_spring = null;

	public string GetHoverName()
	{
		return "";
	}

	public string GetHoverText()
	{
		return "$mb_rope_use";
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	internal SpringJoint GetSpring()
	{
		if (!m_spring)
		{
			m_spring = base.gameObject.AddComponent<SpringJoint>();
		}
		return m_spring;
	}
}
