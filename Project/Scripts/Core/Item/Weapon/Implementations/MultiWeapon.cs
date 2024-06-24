namespace LandlessSkies.Core;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using SevenDev.Utility;


[Tool]
[GlobalClass]
public sealed partial class MultiWeapon : Node, IWeapon, ISerializationListener, IInjectionInterceptor<WeaponHolsterState> {
	private List<IWeapon> _weapons = [];
	public IWeapon? CurrentWeapon {
		get => IndexInBounds(_currentIndex) ? _weapons[_currentIndex] : null;
		private set {
			if (value is not null) {
				_currentIndex = _weapons.IndexOf(value);
				UpdateCurrent();
			}
		}
	}


	public WeaponHolsterState HolsterState { get; set; } = WeaponHolsterState.Unholstered;
	public WeaponType Type => CurrentWeapon?.Type ?? 0;
	public WeaponUsage Usage => CurrentWeapon?.Usage ?? 0;
	public WeaponSize Size => CurrentWeapon?.Size ?? 0;


	public int StyleCount => Mathf.Max(_weapons.Count, 1);

	[ExportGroup("Current Weapon")]
	[Export] public int Style {
		get => _currentIndex;
		set => SwitchTo(value);
	}
	private int _currentIndex;


	public string DisplayName => CurrentWeapon?.DisplayName ?? string.Empty;
	public Texture2D? DisplayPortrait => CurrentWeapon?.DisplayPortrait;


	private MultiWeapon() : base() { }
	public MultiWeapon(IEnumerable<IWeapon?> weapons) : this() {
		Callable.From(() => {
			foreach (Node weaponNode in weapons.OfType<Node>()) {
				weaponNode.ParentTo(weaponNode);
			}
		});
	}
	public MultiWeapon(ImmutableArray<ISaveData<IWeapon>> weaponSaves) : this(weaponSaves.Select(save => save.Load())) { }




	private bool IndexInBounds(int index) => index < _weapons.Count && index >= 0;
	private void UpdateCurrent() {
		if (! IndexInBounds(_currentIndex)) {
			_currentIndex = 0;
		}

		this.PropagateInject(HolsterState);
	}

	public void SwitchTo(IWeapon? weapon) {
		if (weapon is null) return;
		SwitchTo(_weapons.IndexOf(weapon));
	}

	public void SwitchTo(int index) {
		int newIndex = index % StyleCount;
		if (newIndex == _currentIndex && CurrentWeapon is IWeapon currentWeapon) {
			currentWeapon.Style++;
			return;
		}

		_currentIndex = newIndex;

		// Reset style on Weapon to be equipped
		// TODO: if the Entity is a player, check for the preference setting
		// to get the corresponding switch-to-weapon behaviour.
		if (CurrentWeapon is IWeapon newWeapon) {
			newWeapon.Style = 0;
		}

		UpdateCurrent();
	}


	public IEnumerable<AttackBuilder> GetAttacks(Entity target) {
		IWeapon? currentWeapon = CurrentWeapon;
		return _weapons
			.SelectMany((w) => w?.GetAttacks(target) ?? [])
			.Select(a => {
				if (a.Weapon != currentWeapon) {
					a.BeforeExecute += () => SwitchTo(a.Weapon);
					a.AfterExecute += () => SwitchTo(currentWeapon);
				}
				return a;
			});
	}


	public WeaponHolsterState Intercept(Node child, WeaponHolsterState value) =>
		child == CurrentWeapon ? value : WeaponHolsterState.Holstered;

	public List<ICustomization> GetCustomizations() => [];
	public List<ICustomizable> GetSubCustomizables() {
		List<ICustomizable> list = [];
		return [.. list.Concat(_weapons)];
	}
	public ISaveData<IWeapon> Save() {
		return new MultiWeaponSaveData([.. _weapons]);
	}


	private void UpdateWeapons() {
		_weapons = [.. GetChildren().OfType<IWeapon>()];
		UpdateCurrent();
	}

	public override void _Ready() {
		base._Ready();
		UpdateWeapons();
	}
	public override void _Notification(int what) {
		base._Notification(what);
		switch ((ulong)what) {
		case NotificationChildOrderChanged:
			UpdateWeapons();
			break;
		}
	}

	public void OnBeforeSerialize() { }

	public void OnAfterDeserialize() {
		UpdateWeapons();
	}

	[Serializable]
	public class MultiWeaponSaveData(IEnumerable<IWeapon> weapons) : ISaveData<IWeapon> {
		private readonly ISaveData<IWeapon>[] WeaponSaves = weapons
			.Select(w => w.Save())
			.ToArray();

		IWeapon? ISaveData<IWeapon>.Load() => Load();
		public MultiWeapon Load() {
			return new MultiWeapon([.. WeaponSaves]);
		}
	}
}