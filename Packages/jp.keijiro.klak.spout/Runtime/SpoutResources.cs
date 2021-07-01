using UnityEngine;

namespace Klak.Spout {

//
// Spout "Resources" class
// This is used to provide a reference to the shader asset.
//

//[CreateAssetMenu(fileName = "SpoutResources",
//                 menuName = "ScriptableObjects/Klak/Spout/Spout Resources")]
public sealed class SpoutResources : ScriptableObject
{
    public Shader blitShader;
}

} // namespace Klak.Spout
