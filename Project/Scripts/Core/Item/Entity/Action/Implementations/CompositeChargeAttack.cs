using System.Collections.Generic;
using Godot;
using KGySoft.CoreLibraries;
using SevenDev.Utility;

namespace LandlessSkies.Core;

public partial class CompositeChargeAttack(Entity entity, SingleWeapon weapon, CompositeChargeAttackBuilder info, IEnumerable<AttributeModifier> modifiers) : ChargeAttack(entity, weapon, info.ChargeDuration) {


	protected override bool IsChargeStopped(InputDevice inputDevice) {
		return inputDevice.IsActionJustReleased(info.ActionKey);
	}


	protected override void ChargeDone() {
		GD.Print("Charged Up");
	}

	protected override void ChargedAttack() {
		QueueFree();
		Entity.ExecuteAction(new AttackActionInfo(Weapon, info.ChargedAttack), true);
	}

	protected override void UnchargedAttack() {
		QueueFree();
		Entity.ExecuteAction(new AttackActionInfo(Weapon, info.UnchargedAttack), true);
	}


	public override void _Ready() {
		base._Ready();

		Entity.AttributeModifiers.AddRange(modifiers);
	}

	public override void _ExitTree() {
		base._ExitTree();

		Entity.AttributeModifiers.RemoveRange(modifiers);
	}
}