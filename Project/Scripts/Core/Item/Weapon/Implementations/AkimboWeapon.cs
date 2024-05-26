namespace LandlessSkies.Core;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using GodotPlugins.Game;
using SevenDev.Utility;

[Tool]
[GlobalClass]
public sealed partial class AkimboWeapon : Weapon, IInjector<Handedness> {
	[Export] public Weapon? MainWeapon {
		get => _mainWeapon;
		set {
			if (_mainWeapon == value) return;

			_mainWeapon = value?.SafeReparent(this);
			_mainWeapon?.PropagateInject(Skeleton);
			_mainWeapon?.PropagateInject(Handedness);

			MoveChild(_mainWeapon, 0);
		}
	}
	private Weapon? _mainWeapon;

	[Export] public Weapon? SideWeapon {
		get => _sideWeapon;
		set {
			if (_sideWeapon == value) return;

			_sideWeapon = value?.SafeReparent(this);
			_sideWeapon?.PropagateInject(Skeleton);
			_sideWeapon?.PropagateInject(Handedness.Reverse());

			MoveChild(_sideWeapon, 1);
		}
	}
	private Weapon? _sideWeapon;

	public override int StyleCount => (_mainWeapon?.StyleCount ?? base.StyleCount) + (_sideWeapon is null ? 0 : 1);

	public override int Style {
		get => MainWeapon?.Style ?? 0;
		set {
			if (_mainWeapon is not null && value < _mainWeapon.StyleCount) {
				_mainWeapon.Style = value;
			}
			else if (_sideWeapon is not null && value == StyleCount - 1) {
				_sideWeapon.Style++;
			}
		}
	}


	public override Skeleton3D? Skeleton {
		get => base.Skeleton;
		protected set {
			base.Skeleton = value;

			MainWeapon?.PropagateInject(Skeleton);
			SideWeapon?.PropagateInject(Skeleton);
		}
	}

	public override Handedness Handedness {
		get => base.Handedness;
		protected set {
			base.Handedness = value;

			MainWeapon?.PropagateInject(Handedness);
			SideWeapon?.PropagateInject(Handedness.Reverse());
		}
	}


	public override string DisplayName => MainWeapon?.DisplayName ?? string.Empty;
	public override Texture2D? DisplayPortrait => MainWeapon?.DisplayPortrait;


	public override IWeapon.Type WeaponType => MainWeapon?.WeaponType ?? 0;
	public override IWeapon.Usage WeaponUsage => MainWeapon?.WeaponUsage ?? 0;
	public override IWeapon.Size WeaponSize => MainWeapon?.WeaponSize ?? 0;


	private AkimboWeapon() : base() { }
	public AkimboWeapon(Weapon mainWeapon, Weapon sideWeapon) : this() {
		MainWeapon = mainWeapon;
		SideWeapon = sideWeapon;
	}
	public AkimboWeapon(ISaveData<Weapon>? mainSave, ISaveData<Weapon>? sideSave) : this() {
		MainWeapon = mainSave?.Load();
		SideWeapon = sideSave?.Load();
	}

	public override List<ICustomizable> GetSubCustomizables() {
		List<ICustomizable> list = base.GetSubCustomizables();
		if (_mainWeapon is not null) list.Add(_mainWeapon);
		if (_sideWeapon is not null) list.Add(_sideWeapon);
		return list;
	}



	public override IEnumerable<AttackActionInfo> GetAttacks(Entity target) {
		Weapon? currentWeapon = MainWeapon;
		return new List<Weapon?>() {_mainWeapon, _sideWeapon}
			.OfType<Weapon>()
			.SelectMany(w => w.GetAttacks(target));
	}

	public override void Inject(Skeleton3D? skeleton) {
		base.Inject(skeleton);

		_mainWeapon?.PropagateInject(skeleton);
		_sideWeapon?.PropagateInject(skeleton);
	}
	public override void Inject(Handedness handedness) {
		base.Inject(handedness);

		_mainWeapon?.PropagateInject(handedness);
		_sideWeapon?.PropagateInject(handedness.Reverse());
	}

	public Handedness Inject() => Handedness;


	public override void _Notification(int what) {
		base._Notification(what);
		switch ((ulong)what) {
		case NotificationChildOrderChanged:
			Weapon[] weapons = GetChildren().OfType<Weapon>().ToArray();
			Callable.From(() => {
				MainWeapon = weapons.Length > 0 ? weapons[0] : null;
				SideWeapon = weapons.Length > 1 ? weapons[1] : null;
			}).CallDeferred();
			break;
		}
	}


	public override AkimboWeaponSaveData Save() {
		return new AkimboWeaponSaveData(this);
	}


	[Serializable]
	public class AkimboWeaponSaveData(AkimboWeapon akimbo) : ISaveData<Weapon> {
		private readonly ISaveData<Weapon>? MainWeaponSave = akimbo.MainWeapon?.Save();
		private readonly ISaveData<Weapon>? SideWeaponSave = akimbo.SideWeapon?.Save();

		Weapon? ISaveData<Weapon>.Load() => Load();
		public AkimboWeapon Load() {
			return new AkimboWeapon(MainWeaponSave, SideWeaponSave);
		}
	}
}