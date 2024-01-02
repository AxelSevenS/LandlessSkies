#if TOOLS

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace LandlessSkies.Core;


public partial class WeaponInventory {
	private List<WeaponData> _weaponDatas = [];



	[Export] private Array<WeaponData> WeaponDatas {
		get => [.. _weaponDatas];
		set {
			if ( this.IsEditorGetSetter() ) {
				_weaponDatas = [.. value];
				return;
			}
			if (_weaponDatas.SequenceEqual(value)) return;

			int minLength = Math.Min(value.Count, _weaponDatas.Count);
			for (int i = 0; i < Math.Max(value.Count, _weaponDatas.Count); i++) {
				switch (i) {
					case int index when index < minLength && _weaponDatas[index] != value[index]:
						SetWeapon(index, value[index]);
						break;

					case int index when index >= minLength && index < value.Count:
						AddWeapon(value[index]);
						break;

					case int index when index >= minLength && index >= value.Count:
						RemoveWeapon(index);
						break;
				}
			}
			NotifyPropertyListChanged();
		}
	}



	private void ResetData() {
		_weaponDatas = [.. _weapons.Select(weapon => weapon?.Data!)];
		NotifyPropertyListChanged();
	}


	public override void _Ready() {
		base._Ready();

		ResetData();
	}
}
#endif