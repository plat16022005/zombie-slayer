using System.IO;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.Android;

public class FixGradleCoroutines : IPostGenerateGradleAndroidProject
{
    public int callbackOrder { get { return 999; } }

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string rootPath = Directory.GetParent(path).FullName;
        string rootBuildGradle = Path.Combine(rootPath, "build.gradle");
        
        if (File.Exists(rootBuildGradle))
        {
            string content = File.ReadAllText(rootBuildGradle);
            
            // Xóa rác cũ (nếu có)
            string r8Marker = "// --- ANTIGRAVITY R8 PATCH START ---";
            string r8End = "// --- ANTIGRAVITY R8 PATCH END ---";
            if (content.Contains(r8Marker) && content.Contains(r8End))
            {
                int s = content.IndexOf(r8Marker);
                int e = content.IndexOf(r8End) + r8End.Length;
                content = content.Remove(s, e - s);
            }

            string cMarker = "// --- ANTIGRAVITY CORO PATCH START ---";
            string cEnd = "// --- ANTIGRAVITY CORO PATCH END ---";
            if (content.Contains(cMarker) && content.Contains(cEnd))
            {
                int s = content.IndexOf(cMarker);
                int e = content.IndexOf(cEnd) + cEnd.Length;
                content = content.Remove(s, e - s);
            }

            // Gắn code R8 vào ĐẦU FILE (buildscript phải đứng đầu)
            string r8Patch = $@"{r8Marker}
buildscript {{
    repositories {{
        google()
        mavenCentral()
    }}
    dependencies {{
        classpath 'com.android.tools:r8:8.1.56'
    }}
}}
{r8End}";

            // Gắn code Coroutine vào CUỐI FILE
            string coroutinesPatch = $@"{cMarker}
allprojects {{
    configurations.all {{
        resolutionStrategy {{
            force 'org.jetbrains.kotlinx:kotlinx-coroutines-core:1.9.0'
            force 'org.jetbrains.kotlinx:kotlinx-coroutines-core-jvm:1.9.0'
            force 'org.jetbrains.kotlinx:kotlinx-coroutines-android:1.9.0'
            force 'org.jetbrains.kotlinx:kotlinx-coroutines-play-services:1.9.0'
        }}
    }}
}}
{cEnd}";

            // Chỉ gắn khi file chưa bị xóa (có chứa nội dung nguyên thủy của Unity)
            // Lần build đầu tiên Unity sẽ sinh ra nội dung mới, ta chỉ cần nối vào 2 đầu
            content = r8Patch + "\n" + content.Trim() + "\n\n" + coroutinesPatch;
            
            File.WriteAllText(rootBuildGradle, content);
            Debug.Log("ANTIGRAVITY: Đã chèn lệnh nâng cấp R8 và Coroutines an toàn!");
        }
    }
}
