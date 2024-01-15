using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Godot.Collections;


namespace LandlessSkies.Core;


[Tool]
[GlobalClass]
public sealed partial class MultiWeapon : Weapon, IUIObject {
	private Entity? _entity;
	private List<SingleWeapon?> _weapons = [];
	private int _currentIndex = 0;



	[Export] private Array<SingleWeapon?> Weapons {
		get => [.. _weapons];
		set {
			_weapons = [.. value];
			
			if ( this.IsEditorGetSetter() ) return;

			_weapons.ForEach( w => w?.SafeReparentRecursive(this) );
		}
	}

	[ExportGroup("Current Weapon")]
	[Export] private int CurrentIndex {
		get => _currentIndex;
		set {
			if ( this.IsEditorGetSetter() ) {
				_currentIndex = value;
				return;
			}

			SwitchTo(value);
		}
	}


	[ExportGroup("Dependencies")]
	[Export] public Entity? Entity {
		get => _entity;
		set {
			if ( this.IsEditorGetSetter() ) {
				_entity = value;
				return;
			}
			Inject(value);
		}
	}



	public SingleWeapon? CurrentWeapon {
		get => IndexInBounds(CurrentIndex) ? _weapons[CurrentIndex] : null;
		private set {
			if (value is not null) {
				int index = _weapons.IndexOf(value); 
				CurrentIndex = index;
			}
		}
	}

	public override WeaponData Data {
		get => CurrentWeapon?.Data!;
		protected set {
			if ( this.IsEditorGetSetter() ) return;
			SetWeapon(_currentIndex, value);
		}
	}
	public override WeaponCostume? Costume {
		get => CurrentWeapon?.Costume;
		set => CurrentWeapon?.SetCostume(value);
	}


	public override IWeapon.Handedness WeaponHandedness {
		get {
			if ( CurrentWeapon is not SingleWeapon weapon ) {
				return IWeapon.Handedness.Right;
			}

			return weapon.WeaponHandedness;
		}
		set {
			if ( CurrentWeapon is not null ) {
				CurrentWeapon.WeaponHandedness = value;
			}
		}
	}

	public string DisplayName => "Inventory";
	public Texture2D? DisplayPortrait => CurrentWeapon?.UIObject.DisplayPortrait;

	public override IUIObject UIObject => this;
	public override ICustomizable[] Children => [.. base.Children.Concat( _weapons.Cast<ICustomizable>() )];



	private MultiWeapon() : base() {}
	public MultiWeapon(IEnumerable<SingleWeapon> weapons) : this() {
		Weapons = [.. weapons];
	}
	public MultiWeapon(ImmutableArray<KeyValuePair<WeaponData, WeaponCostume?>> weaponsInfo) : this() {
		foreach (KeyValuePair<WeaponData, WeaponCostume?> data in weaponsInfo) {
			AddWeapon(data.Key, data.Value);
		}
	}



	private bool IndexInBounds(int index) => index < _weapons.Count && index >= 0;
	private void UpdateCurrent() {
		if ( ! IndexInBounds(_currentIndex) ) {
			_currentIndex = 0;
		}
		_weapons.ForEach((w) => w?.Disable());
		CurrentWeapon?.Enable();
	}

	public void SwitchTo(SingleWeapon? weapon) =>
		SwitchTo(_weapons.IndexOf(weapon));

	public void SwitchTo(int index) {
		if ( ! IndexInBounds(index) ) return;

		_currentIndex = index;

		UpdateCurrent();
	}


	public void AddWeapon(WeaponData? data, WeaponCostume? costume = null) {
		_weapons.Add(null!);

		SetWeapon(_weapons.Count - 1, data, costume);
	}

	public void SetWeapon(int index, WeaponData? data, WeaponCostume? costume = null) {
		if ( ! IndexInBounds(index) ) return;

		SingleWeapon? weapon = _weapons[index];
		if ( data is not null && data == weapon?.Data ) return;

		new LoadableUpdater<SingleWeapon>(ref weapon, () => data?.Instantiate(costume))
			.BeforeLoad(w => {
				w.SafeReparentRecursive(this);
				w.Inject(Entity);
			})
			.Execute();

		_weapons[index] = weapon!;
		UpdateCurrent();
	}

	public void RemoveWeapon(int index) {
		if ( ! IndexInBounds(index) ) return;

		Weapon? weapon = _weapons[index];
		new LoadableDestructor<Weapon>(ref weapon)
			.Execute();

		_weapons.RemoveAt(index);
		UpdateCurrent();
	}

	public void SetCostume(int index, WeaponCostume? costume) {
		if ( ! IndexInBounds(index) ) return;

		_weapons[index]?.SetCostume(costume);
	}

	public override void SetCostume(WeaponCostume? costume) {
		CurrentWeapon?.SetCostume(costume);
	}


	public override IEnumerable<AttackAction.IInfo> GetAttacks(Entity target) =>
		_weapons
			.SelectMany( (w) => w?.GetAttacks(target) ?? [] )
			.Select( a => {
				SingleWeapon? currentWeapon = CurrentWeapon;
				if (a.Weapon != currentWeapon) {
					a.BeforeExecute += () => SwitchTo(a.Weapon);
					a.AfterExecute += () => SwitchTo(currentWeapon);
				}
				return a;
			});

	public override void Inject(Entity? entity) {
		_entity = entity;

		this.SafeReparentRecursive(entity);

		_weapons.ForEach( w => w?.Inject(entity));
	}


	public override void HandleInput(Player.InputInfo inputInfo) {
		base.HandleInput(inputInfo);
		CurrentWeapon?.HandleInput(inputInfo);
	}

	public override void Enable() => CurrentWeapon?.Enable();
	public override void Disable() => _weapons.ForEach(w => w?.Disable());
	public override void Destroy() => _weapons.ForEach(w => w?.Destroy());
	public override void ReloadModel(bool forceLoad = false) => _weapons.ForEach(w => w?.ReloadModel(forceLoad));

	protected override bool LoadModelImmediate() {
		_weapons.ForEach(w => w?.LoadModel());
		return true;
	}
	protected override bool UnloadModelImmediate() {
		_weapons.ForEach(w => w?.UnloadModel());
		return true;
	}
}