#if TOOLS

namespace LandlessSkies.Core;

using Godot;
using Godot.Collections;

public partial class Character {
	public override bool _PropertyCanRevert(StringName property) {
		if (property == PropertyName.Costume) {
			return Costume != Data?.BaseCostume;
		}
		return base._PropertyCanRevert(property);
	}
	public override Variant _PropertyGetRevert(StringName property) {
		if (property == PropertyName.Costume) {
			return Data?.BaseCostume!;
		}
		return base._PropertyGetRevert(property);
	}

	public override void _ValidateProperty(Dictionary property) {
		base._ValidateProperty(property);

		StringName name = property["name"].AsStringName();

		if (
			name == PropertyName.Collisions ||
			name == PropertyName.Skeleton ||
			name == PropertyName.CharacterModel ||
			(name == PropertyName.Data && Data is not null)
		) {
			property["usage"] = (int) (property["usage"].As<PropertyUsageFlags>() | PropertyUsageFlags.ReadOnly);

		} else if (name == PropertyName.Costume) {
			property["usage"] = (int) (property["usage"].As<PropertyUsageFlags>() & ~PropertyUsageFlags.Storage);

		}
	}
}

#endif