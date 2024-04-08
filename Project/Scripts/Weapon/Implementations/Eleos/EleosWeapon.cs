namespace LandlessSkies.Core;

using System.Collections.Generic;
using Godot;

[Tool]
[GlobalClass]
public sealed partial class EleosWeapon : SingleWeapon<EleosWeaponData> {
	private SlashAttackInfo slashAttack = null!;
	private CompositeChargeAttackInfo chargeAttack = null!;


	public EleosWeapon(EleosWeaponData? data = null, WeaponCostume? costume = null) : base(data, costume) {}
	private EleosWeapon() : base() {}


	public override IEnumerable<AttackInfo> GetAttacks(Entity target) {
		return [
			slashAttack,
			chargeAttack
		];
	}


	public override void HandleInput(CameraController3D cameraController, InputDevice inputDevice) {
		base.HandleInput(cameraController, inputDevice);

		if (! CanProcess()) {
			return;
		}

		if (Entity is null)
			return;

		if (inputDevice.IsActionJustPressed("attack_light")) {
			Entity.ExecuteAction(slashAttack with {});
		}

		if (inputDevice.IsActionJustPressed("attack_heavy")) {
			Entity.ExecuteAction(chargeAttack);
		}
	}

	public override void _Ready() {
		base._Ready();

		slashAttack = new(this);
		chargeAttack = new(this, slashAttack, slashAttack, "attack_heavy");
	}
}