using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;


namespace LandlessSkies.Core;

[Tool]
[GlobalClass]
public abstract partial class Weapon : Loadable3D, IWeapon, IInjectable<Entity?> {
	[Export] public abstract WeaponData Data { get; protected set; }


	[ExportGroup("Costume")]
	[Export] public abstract WeaponCostume? Costume { get; set; }



	public IWeapon.Type WeaponType => Data?.Type ?? 0;
	public abstract IWeapon.Handedness WeaponHandedness { get; set; }

	public virtual IUIObject UIObject => Data;
	public virtual ICustomizable[] Children => [];
	public virtual ICustomizationParameter[] Customizations => [];


	[Signal] public delegate void CostumeChangedEventHandler(WeaponCostume? newCostume, WeaponCostume? oldCostume);



	protected Weapon() : base() {}
	public Weapon(WeaponData data, WeaponCostume? costume, Node3D root) : base(root) {
		ArgumentNullException.ThrowIfNull(data);

		Data = data;
		SetCostume(costume ?? data.BaseCostume);
	}



	public abstract void SetCostume(WeaponCostume? costume);


	public abstract IEnumerable<AttackAction.IAttackInfo> GetAttacks(Entity target);
	public virtual void Inject(Entity? owner) {}


	public virtual void HandleInput(Player.InputInfo inputInfo) {}
	


	public override void _ValidateProperty(Dictionary property) {
		base._ValidateProperty(property);

		switch (property["name"].AsStringName()) {
			case nameof(Data) when Data is not null:
				property["usage"] = (int)(property["usage"].As<PropertyUsageFlags>() | PropertyUsageFlags.ReadOnly);
				break;
		}
	}
}