using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "FPS/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identificação")]
    public string weaponName;
    public Sprite weaponIcon;

    [Header("Prefabs")]
    public GameObject weaponHandPrefab; // Prefab da arma visível na mão do player
    public GameObject weaponDropPrefab; // Prefab para quando a arma for jogada no chão

    [Header("Atributos")]
    public float damage = 25f;
    public float fireRate = 0.2f;
    public int maxAmmoInClip = 30;
    public float range = 100f;
    public bool isAutomatic = false;
}