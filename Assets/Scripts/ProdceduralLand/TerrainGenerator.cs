using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour {

	const float viewerMoveThresholdForChunkUpdate = 25f;
	const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;


	public int colliderLODIndex;
	public LODInfo[] detailLevels;

	public MeshSettings meshSettings;
	public HeightMapSettings heightMapSettings;
	public TextureData textureSettings;

	public Transform viewer;
	public GameObject ghost;
	Transform viewerTwo;
	public Material mapMaterial;
	public float trailDistance = 10000;
	public int originFloats = 0;

	Vector2 viewerPosition;
	Vector2 viewerPositionOld;
	Vector2 viewerTwoPosition;
	Vector2 viewerTwoPositionOld;

	float meshWorldSize;
	int chunksVisibleInViewDst;

	Layers[] things;

	Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
	List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();
	Queue<TerrainChunk> chunkDestroyQue = new Queue<TerrainChunk>();

	public void Start() {
		things = GameObject.FindObjectsOfType<Layers> ();
		Layers ();

		originFloats = 0;
		foreach (TerrainChunk chunk in terrainChunkDictionary.Values) {
			for (int i = chunk.lodMeshes.Length - 1; i > -1; i--) {
				DestroyImmediate (chunk.lodMeshes [i].mesh);
			}
		}
		terrainChunkDictionary.Clear ();
		visibleTerrainChunks.Clear ();
		chunkDestroyQue.Clear ();
		for (int i = transform.childCount - 1; i  > -1; i--) {
			DestroyImmediate (transform.GetChild (i).gameObject);
		}
		if (viewerTwo == null)
			viewerTwo = Instantiate (ghost).transform;

		textureSettings.ApplyToMaterial (mapMaterial);
		textureSettings.UpdateMeshHeights (mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

		float maxViewDst = detailLevels [detailLevels.Length - 1].visibleDstThreshold;
		meshWorldSize = meshSettings.meshWorldSize;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

		heightMapSettings.falloffValues = new float[heightMapSettings.falloffTexture.width, heightMapSettings.falloffTexture.height];
		for (int i = 0; i < heightMapSettings.falloffTexture.width; i++) {
			for (int j = 0; j < heightMapSettings.falloffTexture.height; j++) {
				heightMapSettings.falloffValues [i, j] = heightMapSettings.falloffTexture.GetPixel (i, j).r;
			}
		}

		UpdateVisibleChunks ();
	}

	void Update() {
		if (viewer.position.z > trailDistance / 2) {
			originFloats++;
			Layers ();
			if (viewer.GetComponent<Bus> () != null) {
				viewer.GetComponent<Bus> ().camFollow.transform.position -= Vector3.forward * trailDistance;
				if (viewer.GetComponent<Bus> ().startPoint == 0)
					viewer.GetComponent<Bus> ().currentOffset += (int)(trailDistance / 2f);
				else
					viewer.GetComponent<Bus> ().currentOffset += (int)trailDistance;
				viewer.GetComponent<Bus> ().current = 0;
				viewer.GetComponent<Bus> ().startPoint = (int)(-trailDistance / 2f);

			}
			viewer.position -= Vector3.forward * trailDistance;
		}
		if (viewer.position.z < -trailDistance / 2) {
			originFloats--;
			Layers ();
			if (viewer.GetComponent<Bus> () != null) {
				viewer.GetComponent<Bus> ().camFollow.transform.position += Vector3.forward * trailDistance;
				viewer.GetComponent<Bus> ().currentOffset -= (int)trailDistance;
			}
			viewer.position += Vector3.forward * trailDistance;
		} 

		if (viewer.position.z > 0)
			viewerTwo.transform.position = viewer.transform.position - Vector3.forward * trailDistance;
		else
			viewerTwo.transform.position = viewer.transform.position + Vector3.forward * trailDistance;
		viewerPosition = new Vector2 (viewer.position.x, viewer.position.z);
		viewerTwoPosition = new Vector2 (viewerTwo.position.x, viewerTwo.position.z);

		if (viewerPosition != viewerPositionOld) {
			foreach (TerrainChunk chunk in visibleTerrainChunks) {
				chunk.UpdateCollisionMesh ();
			}
		}

		if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) {
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks ();
		}
		if (chunkDestroyQue.Count > 0) {
			TerrainChunk chunk = chunkDestroyQue.Dequeue ();
			if (terrainChunkDictionary.ContainsKey (chunk.coord) && visibleTerrainChunks.Contains (chunk)) {
				visibleTerrainChunks.Remove (chunk);
			}
			for (int i = chunk.lodMeshes.Length - 1; i > -1; i--) {
				DestroyImmediate (chunk.lodMeshes [i].mesh);
			}
			DestroyImmediate (chunk.meshObject);
			terrainChunkDictionary.Remove (chunk.coord);
		}
	}

	void UpdateVisibleChunks() {
		HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2> ();
		for (int i = visibleTerrainChunks.Count-1; i >= 0; i--) {
			alreadyUpdatedChunkCoords.Add (visibleTerrainChunks [i].coord);
			visibleTerrainChunks [i].UpdateTerrainChunk ();
		}

		int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / meshWorldSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
				if (!alreadyUpdatedChunkCoords.Contains (viewedChunkCoord)) {
					if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
						terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
					} else {
						TerrainChunk newChunk = new TerrainChunk (viewedChunkCoord,trailDistance * originFloats, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer,viewerTwo, mapMaterial, this);
						terrainChunkDictionary.Add (viewedChunkCoord, newChunk);
						newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
						newChunk.Load ();
					}
				}
			}
		}

		if (viewer.position.z > (trailDistance / 2)-200 || viewer.position.z < (-trailDistance / 2)+200)
			StartCoroutine (SecondGen ());
	}

	public void OutDestroy(TerrainChunk a){
		chunkDestroyQue.Enqueue (a);
	}

	void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
		if (isVisible) {
			visibleTerrainChunks.Add (chunk);
		} else {
			visibleTerrainChunks.Remove (chunk);
		}
	}
	IEnumerator SecondGen(){
		HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2> ();
		for (int i = visibleTerrainChunks.Count-1; i >= 0; i--) {
			alreadyUpdatedChunkCoords.Add (visibleTerrainChunks [i].coord);
			visibleTerrainChunks [i].UpdateTerrainChunk ();
		}

		int currentChunkCoordXTwo = Mathf.RoundToInt (viewerTwoPosition.x / meshWorldSize);
		int currentChunkCoordYTwo = Mathf.RoundToInt (viewerTwoPosition.y / meshWorldSize);

		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) {
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) {
				Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordXTwo + xOffset, currentChunkCoordYTwo + yOffset);
				if (!alreadyUpdatedChunkCoords.Contains (viewedChunkCoord)) {
					if (terrainChunkDictionary.ContainsKey (viewedChunkCoord)) {
						terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
					} else {
						float offset = originFloats;
						if (viewer.transform.position.z > 0)
							offset++;
						else
							offset--;
						TerrainChunk newChunk = new TerrainChunk (viewedChunkCoord,trailDistance * (offset), heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer,viewerTwo, mapMaterial, this);
						terrainChunkDictionary.Add (viewedChunkCoord, newChunk);
						newChunk.onVisibilityChanged += OnTerrainChunkVisibilityChanged;
						newChunk.Load ();
					}
				}
				yield return null;
			}
		}
	}
	void Layers(){
		for (int i = 0; i < things.Length; i++) {
			things [i].gameObject.SetActive (things [i].layer == originFloats);
		}
	}
}

[System.Serializable]
public struct LODInfo {
	[Range(0,MeshSettings.numSupportedLODs-1)]
	public int lod;
	public float visibleDstThreshold;


	public float sqrVisibleDstThreshold {
		get {
			return visibleDstThreshold * visibleDstThreshold;
		}
	}
}