using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SceneInfo
{
    public string sceneName;
    public List<GameObject> prefabsToSpawn = new List<GameObject>();
}

