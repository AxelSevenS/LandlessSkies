using Godot;

namespace LandlessSkies.Core;

[Tool]
public abstract partial class Loadable3D : Node3D, ILoadable {
	private bool _isLoaded = false;



	[Export] public bool IsLoaded {
		get => _isLoaded;
		set {
			if ( this.IsEditorGetSetter() ) {
				_isLoaded = value;
				return;
			}

			if ( value ) {
				LoadModel();
			} else {
				UnloadModel();
			}
		}
	}

	public event LoadedUnloadedEventHandler LoadUnloadEvent {
		add => LoadedUnloaded += value;
		remove => LoadedUnloaded -= value;
	}



	[Signal] public delegate void LoadedUnloadedEventHandler(bool isLoaded);



	protected Loadable3D() : base() {
		Name = GetType().Name;
	}
	public Loadable3D(Node3D root) : this() {
		root.AddChildAndSetOwner(this, Engine.IsEditorHint());
	}



	public void LoadModel() {
		if ( IsLoaded ) return;

		if ( ! LoadModelImmediate() ) return;

		_isLoaded = true;
		EmitSignal(SignalName.LoadedUnloaded, true);
	}
	public void UnloadModel() {
		if ( ! IsLoaded ) return;

		if ( ! UnloadModelImmediate() ) return;

		_isLoaded = false;
		EmitSignal(SignalName.LoadedUnloaded, false);
	}
	public virtual void ReloadModel(bool forceLoad = false) {
		bool wasLoaded = IsLoaded;
		UnloadModel();

		if ( wasLoaded || forceLoad ) {
			LoadModel();
		}
	}

	/// <summary>
	/// Loads the model immediately, without checking if it's already loaded.
	/// </summary>
	/// <returns>
	/// Returns true if the model was loaded, false if it wasn't.
	/// </returns>
	protected abstract bool LoadModelImmediate();

	/// <summary>
	/// Unloads the model immediately, without checking if it's already unloaded.
	/// </summary>
	/// <returns>
	/// Returns true if the model was unloaded, false if it wasn't.
	/// </returns>
	protected abstract bool UnloadModelImmediate();

	public virtual void Enable() {
		SetProcess(true);
		Visible = true;
	}
	public virtual void Disable() {
		SetProcess(false);
		Visible = false;
	}
	public virtual void Destroy() {
		this.UnparentAndQueueFree();
	}


	public override void _Notification(int what) {
		base._Notification(what);
		switch((long)what) {
			case NotificationUnparented:
				Callable.From(UnloadModel).CallDeferred();
				break;
			case NotificationParented:
				Callable.From(LoadModel).CallDeferred();
				break;
		}
		// if (what == NotificationUnparented) { // TODO: Wait for NotificationPredelete to be fixed (never lol)
		// 	Callable.From(UnloadModel).CallDeferred();
		// }
	}
}