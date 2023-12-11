using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;


namespace LandlessSkies.Core;


[Tool]
[GlobalClass]
public partial class WeaponInventory : Loadable3D, IWeapon {    

    [Export] private Array<IWeaponWrapper> Weapons {
        get => [.. _weapons];
        set {
            _weapons = value.Select(w => w ?? new(OnWeaponPathChanged)).ToList();
#if TOOLS
            ResetData();
#endif
        }
    }
    private List<IWeaponWrapper> _weapons = [];


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
    private int _currentIndex = 0;

    public IWeapon? CurrentWeapon {
        get => IndexInBounds(CurrentIndex) ? this[CurrentIndex] : null;
        private set {
            IWeaponWrapper? index = _weapons.Where(wrapper => wrapper.Get(this) == value).FirstOrDefault();
            if (value is not null && index is not null) {
                CurrentIndex = _weapons.IndexOf(index);
            }
        }
    }

    public WeaponData Data => CurrentWeapon?.Data!;
    public WeaponCostume? Costume {
        get => CurrentWeapon?.Costume;
        set {
            if (CurrentWeapon is IWeapon currWeapon) {
                currWeapon.Costume = value;
            }
        }
    }
    
    public IWeapon.Type WeaponType => CurrentWeapon?.WeaponType ?? 0;
    public IWeapon.Handedness WeaponHandedness {
        get {
            if ( ! IndexInBounds(CurrentIndex) || this[CurrentIndex] is not IWeapon weapon ) return IWeapon.Handedness.Right;

            return weapon.WeaponHandedness;
        }
    } 


    [ExportGroup("Dependencies")]
    [Export]
    public Skeleton3D? Skeleton { 
        get => _skeleton;
        set => Inject(value);
    }
    private Skeleton3D? _skeleton;


    public IWeapon? this[int index] {
        get => _weapons[index].Get(this);
        set => _weapons[index].Set(this, value);
    }



    private WeaponInventory() : base() {}



    private bool IndexInBounds(int index) {
        return index < _weapons.Count;
    }

    public void SwitchTo(int index) {
        if ( _weapons is null || _weapons.Count == 0 ) {
            _currentIndex = 0;
            return;
        }

        int maxCount = _weapons.Count - 1;
        _currentIndex = index > maxCount ? maxCount : index;

        for (int i = 0; i < _weapons.Count; i++) {
            this[i]?.Disable();
        }

        this[_currentIndex]?.Enable();
    }

    private void OnWeaponPathChanged() {
        Inject(Skeleton);
#if TOOLS
        ResetData();
#endif
    }

    public void AddWeapon(WeaponData? data, WeaponCostume? costume = null) {
        int index = _weapons.Count;

        _weapons.Add(new(OnWeaponPathChanged));
#if TOOLS
        _weaponDatas.Add(null!);
#endif

        SetWeapon(index, data, costume);
    }

    public void SetWeapon(int index, WeaponData? data, WeaponCostume? costume = null) {
        if (index >= _weapons.Count) return;

        IWeaponWrapper weaponWrapper = _weapons[index] ??= new(OnWeaponPathChanged);
        IWeapon? weapon = weaponWrapper.Get(this);
        if ( data is not null && weapon?.Data == data ) return;

        LoadableExtensions.UpdateLoadable(ref weapon!)
            .WithConstructor(() => {
                IWeapon? weapon = data?.Instantiate(this, costume);
                weaponWrapper.Set(this, weapon);
                return weapon;
            })
            .BeforeLoad(() => weapon?.Inject(Skeleton))
            .Execute();

#if TOOLS
        _weaponDatas[index] = data!;
#endif
    }

    public void RemoveWeapon(int index) {
        if (index >= _weapons.Count) return;

        IWeapon? weapon = _weapons[index].Get(this);
        LoadableExtensions.DestroyLoadable(ref weapon)
            .Execute();

        _weapons.RemoveAt(index);
#if TOOLS
        _weaponDatas.RemoveAt(index);
#endif
    }

    public void SetCostume(int index, WeaponCostume? costume) {
        if (index >= _weapons.Count) return;

        if (this[index] is IWeapon weapon) {
            weapon.Costume = costume;
        }
    }

    
    public void Inject(Skeleton3D? skeleton) {
        _skeleton = skeleton;

        for ( int i = 0; i < _weapons?.Count; i++ ) {
            this[i]?.Inject(skeleton);
        }
    }

    public override void Enable() {
        for ( int i = 0; i < _weapons?.Count; i++ ) {
            this[i]?.Enable();
        }
    }

    public override void Disable() {
        for ( int i = 0; i < _weapons?.Count; i++ ) {
            this[i]?.Disable();
        }
    }

    public override void Destroy() {
        for ( int i = 0; i < _weapons?.Count; i++ ) {
            this[i]?.Destroy();
        }
    }

    public override void ReloadModel(bool forceLoad = false) {
        for ( int i = 0; i < _weapons.Count; i++ ) {
            this[i]?.ReloadModel(forceLoad);
        }
    }

    protected override bool LoadModelImmediate() {
        for ( int i = 0; i < _weapons.Count; i++ ) {
            this[i]?.LoadModel();
        }
        return true;
    }

    protected override bool UnloadModelImmediate() {
        for ( int i = 0; i < _weapons.Count; i++ ) {
            this[i]?.UnloadModel();
        }
        return true;
    }

}