using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq;

public class FirebaseManager : MonoBehaviour
{
    private DatabaseReference databaseReference;

    void Start()
    {
        // 初始化 Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log("Firebase 初始化成功！");
                InitializeDatabase();
            }
            else
            {
                Debug.LogError($"Firebase 初始化失敗：{task.Result}");
            }
        });
    }

    void InitializeDatabase()
    {
        // 取得資料庫的根引用
        databaseReference = FirebaseDatabase.DefaultInstance.RootReference;

        // 監聽 exampleNode 資料的變化
        FirebaseDatabase.DefaultInstance
            .GetReference("threads")
            .ValueChanged += HandleValueChanged;

        Debug.Log("資料庫監聽已啟動！");
    }

    void HandleValueChanged(object sender, ValueChangedEventArgs e)
    {
        if (e.DatabaseError != null)
        {
            Debug.LogError($"資料庫監聽錯誤：{e.DatabaseError.Message}");
            return;
        }

        // 確認有數據返回
        if (e.Snapshot != null && e.Snapshot.Value != null)
        {
            // 取得所有資料的字典形式
            var allData = e.Snapshot.Children.ToDictionary(
                child => child.Key, 
                child => child.Value
            );

            // 找到最新的一筆資料（根據 Firebase 的資料順序，最後一個節點通常是最新的）
            var latestChild = e.Snapshot.Children.LastOrDefault();

            if (latestChild != null)
            {
                string latestKey = latestChild.Key;    // 最新資料的 Key
                string latestValue = latestChild.GetRawJsonValue(); // 最新資料的內容

                Debug.Log($"最新資料:{latestKey}");
                Debug.Log($"最新資料內容：{latestValue}");
            }
            else
            {
                Debug.Log("沒有最新的資料。");
            }
        }
        else
        {
            Debug.Log("資料庫中沒有數據。");
        }
    }
}
