using UnityEngine;

public class ParalaxBg : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField, Range(0f, 1f)] float ParalaxSettings = 0.1f;
    [SerializeField] bool DisableVerticalParalax = true;

    // Скрываем эти переменные, чтобы не путаться в инспекторе
    private Transform folowingTarget;
    private Vector3 targetPreviousPosition;

    void Start()
    {
        // 1. Ищем камеру сами
        if (Camera.main != null)
        {
            folowingTarget = Camera.main.transform;
            targetPreviousPosition = folowingTarget.position;
        }
        else
        {
            Debug.LogError("На сцене не найдена Main Camera! Проверь тег камеры.");
        }
    }

    void LateUpdate() // Используем LateUpdate для плавности
    {
        if (folowingTarget == null) return;

        // 2. Считаем, на сколько сдвинулась камера
        Vector3 delta = folowingTarget.position - targetPreviousPosition;

        if (DisableVerticalParalax)
            delta.y = 0;

        
        transform.position += delta * ParalaxSettings;

        
        targetPreviousPosition = folowingTarget.position;
    }
}