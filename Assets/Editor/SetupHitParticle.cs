using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SetupHitParticle
{
    static SetupHitParticle()
    {
        EditorApplication.delayCall += CreateHitParticlePrefab;
    }

    private static void CreateHitParticlePrefab()
    {
        string folderPath = "Assets/Prefabs";
        string prefabPath = folderPath + "/HitImpact_Particle.prefab";

        // Eğer prefab daha önceden oluşturulmuşsa hiçbir işleme gerek yok.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
        {
            return; 
        }

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // Particle System içeren objeyi yarat
        GameObject go = new GameObject("HitImpact_Particle");
        var ps = go.AddComponent<ParticleSystem>();

        // Ana (Main) Ayarlar
        var main = ps.main;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed = 15f;
        main.stopAction = ParticleSystemStopAction.Destroy;
        // İsteğe göre rengi sudaki renklere yakınlaştırabiliriz (Beyaz/Soluk Mavi gibi)
        main.startColor = new Color(0.8f, 1f, 1f, 1f); 

        // Yayın (Emission) Ayarları
        var emission = ps.emission;
        emission.rateOverTime = 0f; // Sürekli patlamasın
        // Tek seferde GÜM diye 15 parça atsın
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

        // Şekil (Shape) Ayarları
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        // Boyut Zamanla Küçülsün (Size over Lifetime)
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // Prefab olarak kaydet
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        
        // Sahnedeki temp objeyi temizle
        Object.DestroyImmediate(go);

        Debug.Log("Sistemin Otomatik Mesajı: HitImpact_Particle prefab'ı 'Assets/Prefabs' klasörüne yapıldı!");
    }
}
