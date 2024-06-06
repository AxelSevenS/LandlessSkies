namespace LandlessSkies.Core;

using System;
using Godot;
using SevenDev.Utility;

[Tool]
[GlobalClass]
public partial class DrawAndComputeCompositorEffect : BaseCompositorEffect {
	public static readonly StringName Context = "UnderwaterEffect";
	public static readonly StringName WaterMapName = "water_map";
	public static readonly StringName WaterDepthName = "water_depth";


	[Export] private RDShaderFile? RenderShaderFile {
		get => _renderShaderFile;
		set {
			_renderShaderFile = value;

			if (RenderingDevice is not null) {
				Destruct();
				Construct();
			}
		}
	}
	private RDShaderFile? _renderShaderFile;
	private Rid renderShader;

	[Export] private RDShaderFile? ComputeShaderFile {
		get => _computeShaderFile;
		set {
			_computeShaderFile = value;

			if (RenderingDevice is not null) {
				Destruct();
				Construct();
			}
		}
	}
	private RDShaderFile? _computeShaderFile;
	private Rid computeShader;

	private Rid nearestSampler;

	private readonly RDAttachmentFormat waterMapAttachmentFormat = new() {
		Format = RenderingDevice.DataFormat.R16G16B16A16Unorm,
		Samples = RenderingDevice.TextureSamples.Samples1,
		UsageFlags = (uint)(RenderingDevice.TextureUsageBits.ColorAttachmentBit | RenderingDevice.TextureUsageBits.StorageBit)
	};
	private readonly RDAttachmentFormat waterDepthAttachmentFormat = new() {
		Format = RenderingDevice.DataFormat.D32Sfloat,
		Samples = RenderingDevice.TextureSamples.Samples1,
		UsageFlags = (uint)RenderingDevice.TextureUsageBits.DepthStencilAttachmentBit
	};
	private long framebufferFormat;

	private readonly RDVertexAttribute vertexAttribute = new() {
		Format = RenderingDevice.DataFormat.R32G32B32Sfloat,
		Location = 0,
		Stride = sizeof(float) * 3,
	};
	private long vertexFormat;

	private Rid renderPipeline;
	private Rid computePipeline;



	public DrawAndComputeCompositorEffect() : base() {
		EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
	}



	public override void _RenderCallback(int effectCallbackType, RenderData renderData) {
		base._RenderCallback(effectCallbackType, renderData);

		// if (effectCallbackType != (long)EffectCallbackTypeEnum.PostTransparent) return;

		if (RenderingDevice is null || _renderShaderFile is null || _computeShaderFile is null) return;


		RenderSceneBuffers renderSceneBuffers = renderData.GetRenderSceneBuffers();
		if (renderSceneBuffers is not RenderSceneBuffersRD sceneBuffers) return;
		RenderSceneData renderSceneData = renderData.GetRenderSceneData();
		if (renderSceneData is not RenderSceneDataRD sceneData) return;

		uint viewCount = sceneBuffers.GetViewCount();


		Vector2I renderSize = sceneBuffers.GetInternalSize();
		if (renderSize.X == 0.0 && renderSize.Y == 0.0) {
			throw new ArgumentException("Render size is incorrect");
		}

		uint xGroups = (uint)((renderSize.X - 1) / 8) + 1;
		uint yGroups = (uint)((renderSize.Y - 1) / 8) + 1;


		if (sceneBuffers.HasTexture(Context, WaterMapName)) {
			// Reset the Color and Depth textures if their sizes are wrong
			RDTextureFormat textureFormat = sceneBuffers.GetTextureFormat(Context, WaterMapName);
			if (textureFormat.Width != renderSize.X || textureFormat.Height != renderSize.Y) {
				sceneBuffers.ClearContext(Context);
			}
		}

		if (! sceneBuffers.HasTexture(Context, WaterMapName)) {
			// Create and cache the Map and Depth to create the Water Buffer
			sceneBuffers.CreateTexture(Context, WaterMapName, waterMapAttachmentFormat.Format, waterMapAttachmentFormat.UsageFlags, waterMapAttachmentFormat.Samples, renderSize, viewCount, 1, true);
			sceneBuffers.CreateTexture(Context, WaterDepthName, waterDepthAttachmentFormat.Format, waterDepthAttachmentFormat.UsageFlags, waterDepthAttachmentFormat.Samples, renderSize, viewCount, 1, true);
		}


		Color[] clearColors = [new Color(0, 0, 0, 0)];


		for (uint view = 0; view < viewCount; view++) {
			Rid waterMap = sceneBuffers.GetTextureSlice(Context, WaterMapName, view, 0, 1, 1);
			Rid waterDepth = sceneBuffers.GetTextureSlice(Context, WaterDepthName, view, 0, 1, 1);

			// Include the Map and Depth from earlier
			Rid waterBuffer = RenderingDevice.FramebufferCreate([waterMap, waterDepth], framebufferFormat);
			if (! waterBuffer.IsValid) {
				throw new ArgumentException("Water Mask Frame Buffer is Invalid");
			}

			// TODO: get the mesh data instead of this hard-coded data
			float[] vertCoords = [
				5f,	0f,	0f,
				0f,	0f,	0f,
				0f,	5f,	0f,
				5f,	5f,	0f,
				5f,	5f,	-5f,
				5f,	0f,	-5f,
			];
			uint[] vertIndices = [
				0, 1, 2,
				0, 2, 3,
				0, 3, 4,
				0, 4, 5,
			];

			(_, Rid vertexArray) = RenderingDevice.VertexArrayCreate(vertCoords, vertexFormat);
			(_, Rid indexArray) = RenderingDevice.IndexArrayCreate(vertIndices);


			Projection projection = sceneData.GetViewProjection(view);
			Projection viewMatrix = new(sceneData.GetCamTransform().Inverse());

			// World-space -> Clip-space Matrix to be used in the rendering shader
			Projection WorldToClip = projection * viewMatrix;
			// Eye Offset for fancy VR multi-view
			Vector3 eyeOffset = sceneData.GetViewEyeOffset(view);

			// Unfolding into a push constant
			float[] renderPushConstant = [
				WorldToClip.X.X, WorldToClip.X.Y, WorldToClip.X.Z, WorldToClip.X.W,
				WorldToClip.Y.X, WorldToClip.Y.Y, WorldToClip.Y.Z, WorldToClip.Y.W,
				WorldToClip.Z.X, WorldToClip.Z.Y, WorldToClip.Z.Z, WorldToClip.Z.W,
				WorldToClip.W.X, WorldToClip.W.Y, WorldToClip.W.Z, WorldToClip.W.W,

				eyeOffset.X, eyeOffset.Y, eyeOffset.Z, 0, // Pad with a zero, because the push constant needs to contain a multiple of 16 bytes (4 floats)
			];
			byte[] renderPushConstantBytes = renderPushConstant.ToByteArray();


			// Render the Geometry (see vertCoords and vertIndices) to an intermediate framebuffer To use later
			RenderingDevice.DrawCommandBeginLabel("Render Water Mask", new Color(1f, 1f, 1f));
			long drawList = RenderingDevice.DrawListBegin(waterBuffer, RenderingDevice.InitialAction.Clear, RenderingDevice.FinalAction.Store, RenderingDevice.InitialAction.Clear, RenderingDevice.FinalAction.Discard, clearColors);
			RenderingDevice.DrawListBindRenderPipeline(drawList, renderPipeline);
			RenderingDevice.DrawListBindVertexArray(drawList, vertexArray);
			RenderingDevice.DrawListBindIndexArray(drawList, indexArray);
			RenderingDevice.DrawListSetPushConstant(drawList, renderPushConstantBytes, (uint)renderPushConstantBytes.Length);
			RenderingDevice.DrawListDraw(drawList, true, 2);
			RenderingDevice.DrawListEnd();
			RenderingDevice.DrawCommandEndLabel();

			// Unfolding into a push constant
			float[] computePushConstant = [
				renderSize.X, renderSize.Y, 0, 0, // Pad instead with two zeroes here
			];
			byte[] computePushConstantBytes = computePushConstant.ToByteArray();

			// Here we draw the Underwater effect, using the waterBuffer to know where there is water geometry
			RenderingDevice.DrawCommandBeginLabel("Render Underwater Effect", new Color(1f, 1f, 1f));
			long computeList = RenderingDevice.ComputeListBegin();
			RenderingDevice.ComputeListBindComputePipeline(computeList, computePipeline);
			RenderingDevice.ComputeListBindColor(computeList, computeShader, sceneBuffers, view, 0);
			RenderingDevice.ComputeListBindDepth(computeList, computeShader, sceneBuffers, view, nearestSampler, 1);
			RenderingDevice.ComputeListBindImage(computeList, computeShader, waterMap, 2);
			RenderingDevice.ComputeListSetPushConstant(computeList, computePushConstantBytes, (uint)computePushConstantBytes.Length);
			RenderingDevice.ComputeListDispatch(computeList, xGroups, yGroups, 1);
			RenderingDevice.ComputeListEnd();
			RenderingDevice.DrawCommandEndLabel();


			RenderingDevice.FreeRid(vertexArray);
			RenderingDevice.FreeRid(indexArray);

			RenderingDevice.FreeRid(waterBuffer);
		}
	}




	protected override void ConstructBehaviour(RenderingDevice renderingDevice) {
		// Framebuffer Format includes a depth attachment to Self-occlude
		framebufferFormat = renderingDevice.FramebufferFormatCreate([waterMapAttachmentFormat, waterDepthAttachmentFormat]);
		vertexFormat = renderingDevice.VertexFormatCreate([vertexAttribute]);

		nearestSampler = renderingDevice.SamplerCreate(new() {
			MinFilter = RenderingDevice.SamplerFilter.Nearest,
			MagFilter = RenderingDevice.SamplerFilter.Nearest
		});

		ConstructRenderPipeline(renderingDevice);
		ConstructComputePipeline(renderingDevice);
	}

	private void ConstructRenderPipeline(RenderingDevice renderingDevice) {
		if (RenderShaderFile is null) return;

		renderShader = renderingDevice.ShaderCreateFromSpirV(RenderShaderFile.GetSpirV());
		if (! renderShader.IsValid) {
			throw new ArgumentException("Render Shader is Invalid");
		}


		RDPipelineColorBlendState blend = new() {
			Attachments = [new RDPipelineColorBlendStateAttachment()]
		};

		renderPipeline = renderingDevice.RenderPipelineCreate(
			renderShader,
			framebufferFormat,
			vertexFormat,
			RenderingDevice.RenderPrimitive.Triangles,
			new RDPipelineRasterizationState(),
			new RDPipelineMultisampleState(),
			new RDPipelineDepthStencilState() {
				// Enable Self-occlusion via Depth Test (see ConstructBehaviour(RenderingDevice))
				EnableDepthTest = true,
				EnableDepthWrite = true,
				DepthCompareOperator = RenderingDevice.CompareOperator.LessOrEqual
			},
			blend
		);
		if (! renderPipeline.IsValid) {
			throw new ArgumentException("Render Pipeline is Invalid");
		}
	}


	private void ConstructComputePipeline(RenderingDevice renderingDevice) {
		if (ComputeShaderFile is null) return;

		computeShader = renderingDevice.ShaderCreateFromSpirV(ComputeShaderFile.GetSpirV());
		if (! computeShader.IsValid) {
			throw new ArgumentException("Compute Shader is Invalid");
		}

		computePipeline = renderingDevice.ComputePipelineCreate(computeShader);
		if (! computePipeline.IsValid) {
			throw new ArgumentException("Compute Pipeline is Invalid");
		}
	}


	protected override void DestructBehaviour(RenderingDevice renderingDevice) {
		if (nearestSampler.IsValid) {
			renderingDevice.FreeRid(nearestSampler);
		}

		if (renderShader.IsValid) {
			renderingDevice.FreeRid(renderShader);
			renderShader = default;
		}
		// Don't need to free the pipeline as freeing the shader does that for us.
		renderPipeline = default;

		if (computeShader.IsValid) {
			renderingDevice.FreeRid(computeShader);
			computeShader = default;
		}
		// Same as above
		computePipeline = default;
	}
}