using System;
using System.Runtime.CompilerServices;
using Godot;


namespace LandlessSkies.Core;

public static class EngineUtils {

#if TOOLS
	private static readonly ulong buildFrame = 0;



	static EngineUtils() {
		buildFrame = Engine.GetProcessFrames();
	}
#endif



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool JustBuilt(this Node node) =>
#if TOOLS
		node.IsNodeReady() && Engine.GetProcessFrames() == buildFrame;
#else
		false;
#endif

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsEditorGetSetter(this Node node) =>
#if TOOLS
		!node.IsNodeReady() || Engine.GetProcessFrames() == buildFrame;
#else
		false;
#endif

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsEditorEnterTree(this Node node) =>
#if TOOLS
		node.IsNodeReady() || Engine.GetProcessFrames() == buildFrame; // TODO: Make this return true when switching scene
#else
		false;
#endif

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsEditorExitTree(this Node node) =>
#if TOOLS
		!node.IsNodeReady() || Engine.GetProcessFrames() == buildFrame; // TODO: Make this return true when switching scene
#else
		false;
#endif



	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void AddChildAndSetOwner(this Node obj, Node child, bool forceReadableName = false) {
		obj.AddChild(child, forceReadableName);
		child.Owner = obj.Owner;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TNode ParentTo<TNode>(this TNode child, Node parent) where TNode : Node {
		parent.AddChild(child);
		return child;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TNode SetOwnerAndParentTo<TNode>(this TNode child, Node parent) where TNode : Node {
		parent.AddChildAndSetOwner(child);
		return child;
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TNode Unparent<TNode>(this TNode child) where TNode : Node {
		child.GetParent()?.RemoveChild(child);
		return child;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void UnparentAndQueueFree(this Node obj) {
		obj.QueueFree();
		obj.GetParent()?.RemoveChild(obj);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T? GetNodeByTypeName<T>(this Node obj) where T : Node {
		return obj.GetNodeOrNull<T>(typeof(T).Name);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryGetNode<T>(this Node obj, NodePath nodePath, out T node) where T : class {
		node = default!;
		if ( ! obj.HasNode(nodePath) ) return false;

		if ( obj.GetNodeOrNull(nodePath) is T tNode ) {
			node = tNode;
			return true;
		}

		return false;
	}


	public static void PackWithSubnodes(this PackedScene scene, Node path) {
		ReownChildren(path);
		void ReownChildren(Node node, uint layer = 0) {
			foreach (Node item in node.GetChildren()) {
				Node currentOwner = item.Owner;
				Callable.From(() => item.Owner = currentOwner).CallDeferred();

				item.Owner = path;
				ReownChildren(item, layer + 1);
			}
		}

		scene.Pack(path);
	}

	public static void MakeLocal(this Node node, Node owner) {
		node.SceneFilePath = string.Empty;
		node.Owner = owner;
		foreach(Node childNode in node.GetChildren()) {
			MakeLocal(childNode, owner);
		}
	}

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred(this Action action) =>
		Callable.From(action).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0>(this Action<T0> action) =>
		Callable.From(action).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1>(this Action<T0, T1> action) =>
		Callable.From(action).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2>(this Action<T0, T1, T2> action) =>
		Callable.From(action).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3>(this Action<T0, T1, T2, T3> action) =>
		Callable.From(action).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4>(this Action<T0, T1, T2, T3, T4> action) =>
		Callable.From(action).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5>(this Action<T0, T1, T2, T3, T4, T5> action) =>
		Callable.From(action).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5, [MustBeVariant] T6>(this Action<T0, T1, T2, T3, T4, T5, T6> action) =>
		Callable.From(action).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5, [MustBeVariant] T6, [MustBeVariant] T7>(this Action<T0, T1, T2, T3, T4, T5, T6, T7> action) =>
		Callable.From(action).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5, [MustBeVariant] T6, [MustBeVariant] T7, [MustBeVariant] T8>(this Action<T0, T1, T2, T3, T4, T5, T6, T7, T8> action) =>
		Callable.From(action).CallDeferred();


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] TResult>(this Func<TResult> func) =>
		Callable.From(func).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] TResult>(this Func<T0, TResult> func) =>
		Callable.From(func).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] TResult>(this Func<T0, T1, TResult> func) =>
		Callable.From(func).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] TResult>(this Func<T0, T1, T2, TResult> func) =>
		Callable.From(func).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] TResult>(this Func<T0, T1, T2, T3, TResult> func) =>
		Callable.From(func).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] TResult>(this Func<T0, T1, T2, T3, T4, TResult> func) =>
		Callable.From(func).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5, [MustBeVariant] TResult>(this Func<T0, T1, T2, T3, T4, T5, TResult> func) =>
		Callable.From(func).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5, [MustBeVariant] T6, [MustBeVariant] TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, TResult> func) =>
		Callable.From(func).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5, [MustBeVariant] T6, [MustBeVariant] T7, [MustBeVariant] TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, TResult> func) =>
		Callable.From(func).CallDeferred();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void CallDeferred<[MustBeVariant] T0, [MustBeVariant] T1, [MustBeVariant] T2, [MustBeVariant] T3, [MustBeVariant] T4, [MustBeVariant] T5, [MustBeVariant] T6, [MustBeVariant] T7, [MustBeVariant] T8, [MustBeVariant] TResult>(this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, TResult> func) =>
		Callable.From(func).CallDeferred();

}