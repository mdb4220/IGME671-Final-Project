using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Spell")]
public class Spell : ScriptableObject
{
    new public string name;
    public GameObject prefab;
    public float manacost;
    public int animtype;
}
