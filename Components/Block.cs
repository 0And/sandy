using Godot;
using System;

public class Block : StaticBody
{
	public int BlockNumber { get; set; }
	public const float FallSpeed = 1;
	public const float FallLimit = 10;
	public const float LimitFallSpeed = 100;
	public const float QueueFreeLimit = -200;
	public Color[] BlockColors = new Color[]
	{
		new Color(0.035294f, 0.062745f, 0.101961f), // Light Grey: #??????
		new Color(0.172549f, 0.219608f, 0.294118f), // Dark Grey: #??????
	};
	public Color TrunkColor = new Color(0.086275f, 0.058824f, 0.047059f); // Brown: #160f0c
	public Color[] LeafColors = new Color[]
	{
		new Color(0.25098f, 0.52549f, 0.321569f), // Bright Green: #408652
		new Color(0.533333f, 0.27451f, 0.584314f), // Bright Pink: #884695
	};

	private bool _fall = false;

	private Godot.Collections.Array GenerateMeshArray(int rings, int segments, float radius, float ringHeight, float randomness = 0, Curve curve = null)
	{
		const int triIndexCount = 3;
		const int quadIndexCount = 6;
		float pointRadianDistance = Mathf.Tau / segments;
		Vector3[] vertices = new Vector3[rings * segments];
		Vector3[] normals = new Vector3[rings * segments];
		Vector2[] uvs = new Vector2[rings * segments];
		int sideTriangleIndices = (rings - 1) * segments * quadIndexCount;
		int topTriangleIndices = triIndexCount * (segments - 2);
		int bottomTriangleIndices = triIndexCount * (segments - 2);
		int[] indices = new int[sideTriangleIndices + topTriangleIndices + bottomTriangleIndices];
		float[] previousRandomness = new float[segments];
		for (int r = 0; r < rings; r++)
		{
			float trueRadius = curve != null ? curve.Interpolate((float) r / (rings - 1)) * radius : radius;
			for (int s = 0; s < segments; s++)
			{
				if (randomness > 0)
				{
					float randomNumber = (float) GD.RandRange(-randomness / 2, randomness / 2);
					if (r > 0 || s > 0)
					{
						float previousRandomNumber = previousRandomness[s > 0 ? s - 1 : segments - 1];
						randomNumber += r > 0 ? (previousRandomNumber + previousRandomness[s]) / 2 : previousRandomNumber;
					}
					previousRandomness[s] = randomNumber;
					trueRadius = Mathf.Max(randomness / 2, trueRadius + randomNumber);
				}
				int vertexIndex = r * segments + s;
				vertices[vertexIndex] = new Vector3(Mathf.Cos(s * pointRadianDistance) * (trueRadius), r * ringHeight, Mathf.Sin(s * pointRadianDistance) * trueRadius);
				normals[vertexIndex] = vertices[vertexIndex].Normalized();
				float v = (float) s / segments;
				float u = (float) r / rings;
				uvs[vertexIndex] = new Vector2(u, v);
				if (r > 0 && s > 0)
				{
					int indexIndex = (r - 1) * segments * quadIndexCount + (s - 1) * quadIndexCount;
					indices[indexIndex] = vertexIndex - segments - 1;
					indices[indexIndex + 1] = vertexIndex - segments;
					indices[indexIndex + 2] = vertexIndex - 1;
					indices[indexIndex + 3] = vertexIndex - 1;
					indices[indexIndex + 4] = vertexIndex - segments;
					indices[indexIndex + 5] = vertexIndex;
					if (s == segments - 1)
					{
						int origin = vertexIndex - (2 * segments) + 1;
						indexIndex += quadIndexCount;
						indices[indexIndex] = vertexIndex - segments;
						indices[indexIndex + 1] = origin;
						indices[indexIndex + 2] = vertexIndex;
						indices[indexIndex + 3] = vertexIndex;
						indices[indexIndex + 4] = origin;
						indices[indexIndex + 5] = origin + segments;
					}
				}
			}
		}
		// Top + Bottom Indices
		int tris = topTriangleIndices / triIndexCount;
		// Bottom
		for (int i = 0; i < tris; i++)
		{
			int bottomOrigin = segments - 1;
			indices[sideTriangleIndices + i * triIndexCount] = bottomOrigin;
			indices[sideTriangleIndices + i * triIndexCount + 1] = bottomOrigin - i - 1;
			indices[sideTriangleIndices + i * triIndexCount + 2] = bottomOrigin - i - 2;
		}
		// Top
		for (int i = 0; i < tris; i++)
		{
			int topOrigin = (rings - 1) * segments;
			indices[sideTriangleIndices + bottomTriangleIndices + i * triIndexCount] = topOrigin;
			indices[sideTriangleIndices + bottomTriangleIndices + i * triIndexCount + 1] = topOrigin + i + 1;
			indices[sideTriangleIndices + bottomTriangleIndices + i * triIndexCount + 2] = topOrigin + i + 2;
		}

		Godot.Collections.Array meshArray = new Godot.Collections.Array();
		meshArray.Resize((int) ArrayMesh.ArrayType.Max);
		meshArray[(int) ArrayMesh.ArrayType.Vertex] = vertices;
		meshArray[(int) ArrayMesh.ArrayType.TexUv] = uvs;
		meshArray[(int) ArrayMesh.ArrayType.Index] = indices;
		meshArray[(int) ArrayMesh.ArrayType.Normal] = normals;
		return meshArray;
	}

	private void GenerateTree(Vector3 treePosition)
	{
		// Trunk
		float yRotation = (float) GD.RandRange(0, Mathf.Tau);
		int trunkRings = 2;
		int trunkSegments = 6;
		float trunkRadius = (float) GD.RandRange(0.2f, 0.3f);
		float trunkHeight = (float) GD.RandRange(2, 3);
		// Trunk Collision
		Godot.Collections.Array trunkArray = GenerateMeshArray(trunkRings, trunkSegments, trunkRadius, trunkHeight);
		Vector3[] trunkVertices = (Vector3[]) trunkArray[(int) ArrayMesh.ArrayType.Vertex];
		CollisionShape trunkCol = new CollisionShape();
		ConvexPolygonShape trunkShape = new ConvexPolygonShape();
		trunkShape.Points = trunkVertices;
		trunkCol.Shape = trunkShape;
		trunkCol.Translation = treePosition;
		trunkCol.RotateY(yRotation);
		AddChild(trunkCol);
		// Trunk Material
		SpatialMaterial trunkMat = new SpatialMaterial();
		trunkMat.AlbedoColor = TrunkColor;
		trunkMat.ParamsDiffuseMode = SpatialMaterial.DiffuseMode.Toon;
		trunkMat.ParamsSpecularMode = SpatialMaterial.SpecularMode.Toon;
		// Trunk Mesh
		ArrayMesh trunkMesh = new ArrayMesh();
		trunkMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, trunkArray);
		trunkMesh.SurfaceSetMaterial(0, trunkMat);
		MeshInstance trunkMeshInstance = new MeshInstance();
		trunkMeshInstance.Mesh = trunkMesh;
		trunkCol.AddChild(trunkMeshInstance);

		// Leaf
		int leafRings = 2;
		int leafSegments = 4;
		float leafRadius = trunkRadius + (float) GD.RandRange(0.4f, 0.8f);
		float leafHeight = (float) GD.RandRange(3, 4);
		// Leaf Collision
		Godot.Collections.Array leafArray = GenerateMeshArray(leafRings, leafSegments, leafRadius, leafHeight);
		Vector3[] leafVertices = (Vector3[]) leafArray[(int) ArrayMesh.ArrayType.Vertex];
		CollisionShape leafCol = new CollisionShape();
		ConvexPolygonShape leafShape = new ConvexPolygonShape();
		leafShape.Points = leafVertices;
		leafCol.Shape = leafShape;
		leafCol.Translation = treePosition + new Vector3(0, trunkHeight, 0);
		leafCol.RotateY(yRotation);
		AddChild(leafCol);
		// Leaf Material
		SpatialMaterial leafMat = new SpatialMaterial();
		leafMat.AlbedoColor = LeafColors[GD.Randi() % LeafColors.Length];
		leafMat.ParamsDiffuseMode = SpatialMaterial.DiffuseMode.Toon;
		leafMat.ParamsSpecularMode = SpatialMaterial.SpecularMode.Toon;
		// Leaf Mesh
		ArrayMesh leafMesh = new ArrayMesh();
		leafMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, leafArray);
		leafMesh.SurfaceSetMaterial(0, leafMat);
		MeshInstance leafMeshInstance = new MeshInstance();
		leafMeshInstance.Mesh = leafMesh;
		leafCol.AddChild(leafMeshInstance);
	}

	public void GenerateIsland(Curve[] pCurves = null, int maxTreeCount = 4)
	{
		// Generate Vertices
		int rings = 12;
		int segments = 12;
		float radius = 5;
		float ringHeight = 0.3f;
		float randomness = 0.1f;
		Curve curve = null;
		if (pCurves != null)
		{
			uint curveNumber = GD.Randi() % (uint) pCurves.Length;
			curve = pCurves[curveNumber];
			ringHeight = curveNumber == 0 ? 0.4f : 0.3f;
		}
		Godot.Collections.Array islandArray = GenerateMeshArray(rings, segments, radius, ringHeight, randomness, curve);
		Vector3[] islandVertices = (Vector3[]) islandArray[(int) ArrayMesh.ArrayType.Vertex];
		// Add Collision
		CollisionShape islandCollision = new CollisionShape();
		ConvexPolygonShape islandShape = new ConvexPolygonShape();
		islandShape.Points = islandVertices;
		islandCollision.Shape = islandShape;
		islandCollision.Translation -= new Vector3(0, rings * ringHeight, 0);
		AddChild(islandCollision);
		// Add Material
		SpatialMaterial islandMaterial = new SpatialMaterial();
		Gradient islandGradient = new Gradient();
		islandGradient.Colors = BlockColors;
		islandGradient.Offsets = new float[2] { 0, 1 };
		GradientTexture islandTexture = new GradientTexture();
		islandTexture.Gradient = islandGradient;
		islandMaterial.AlbedoTexture = islandTexture;
		islandMaterial.ParamsDiffuseMode = SpatialMaterial.DiffuseMode.Toon;
		islandMaterial.ParamsSpecularMode = SpatialMaterial.SpecularMode.Toon;
		// Add Mesh
		ArrayMesh islandMesh = new ArrayMesh();
		islandMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, islandArray);
		islandMesh.SurfaceSetMaterial(0, islandMaterial);
		MeshInstance islandMeshInstance = new MeshInstance();
		islandMeshInstance.Mesh = islandMesh;
		islandCollision.AddChild(islandMeshInstance);
		// Generate Trees
		float boundsLimit = 1.5f;
		float verticalPosition = -1;
		for (int i = 0; i < GD.Randi() % (maxTreeCount + 1); i++)
		{
			Vector2 horizontalPosition = new Vector2(1, 0).Rotated((float)GD.RandRange(0, Mathf.Tau)) * ((float)GD.RandRange(0, radius) - boundsLimit);
			GenerateTree(new Vector3(horizontalPosition.x, verticalPosition, horizontalPosition.y));
		}
	}

	public void UpdateScore(int score)
	{
		if (score >= BlockNumber)
		{
			_fall = true;
		}
	}

	public override void _PhysicsProcess(float delta)
	{
		if (_fall)
		{
			if (GlobalTransform.origin.y > FallLimit)
			{
				Translation -= new Vector3(0, FallSpeed * delta, 0);
			}
			else if (GlobalTransform.origin.y > QueueFreeLimit)
			{
				Translation -= new Vector3(0, LimitFallSpeed * delta, 0);
			}
			else
			{
				QueueFree();
			}
		}
	}
}