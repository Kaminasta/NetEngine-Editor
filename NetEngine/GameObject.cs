using NetEngine.Components;
using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.Serialization;

namespace NetEngine;

public class GameObject : Object
{
    [JsonProperty]
    private List<Component> _components = new();

    public string Name;
    public string Tag;
    public string Layer;
    public bool IsActive = true;

    public Transform transform;

    public GameObject(string name) : this() 
    {
        this.Name = name;
    }

    public GameObject()
    {
        NetEngine.Console.EditorLog("[GameObject] Создан экземпляр GameObject");

        if (transform == null)
            transform = new Transform(this);

        if (!_components.Any(c => c is Transform))
            _components.Add(transform);

        try
        {
            HotReloadManager.RegisterGameObject(this);
            NetEngine.Console.EditorLog("[GameObject] GameObject зарегистрирован в HotReloadManager");
        }
        catch (Exception ex)
        {
            NetEngine.Console.EditorError($"[GameObject] Ошибка при регистрации в HotReloadManager: {ex.Message}");
        }
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        NetEngine.Console.EditorLog("[GameObject - OnDeserialized] Начинается десериализация GameObject");

        int removedCount = _components.RemoveAll(c => c is Transform && !ReferenceEquals(c, transform));
        if (removedCount > 0)
            NetEngine.Console.EditorWarning($"[GameObject - OnDeserialized] Удалено {removedCount} лишних компонентов Transform");

        if (transform != null)
        {
            transform.GameObject = this;

            if (!_components.Contains(transform))
            {
                _components.Add(transform);
                NetEngine.Console.EditorWarning("[GameObject - OnDeserialized] Transform был отсутствует в списке компонентов и добавлен");
            }
        }
        else
        {
            NetEngine.Console.EditorError("[GameObject - OnDeserialized] Transform равен null после десериализации");
        }
    }

    public T AddComponent<T>() where T : Component, new()
    {
        if (_components.Any(c => c.GetType() == typeof(T)))
        {
            NetEngine.Console.EditorWarning($"Попытка добавить компонент типа {typeof(T).Name}, который уже существует");
            return GetComponent<T>()!;
        }

        var component = new T { GameObject = this };
        _components.Add(component);
        NetEngine.Console.EditorLog($"Компонент {typeof(T).Name} добавлен");
        return component;
    }

    public Component AddComponent(Type type)
    {
        if (_components.Any(c => c.GetType() == type))
        {
            NetEngine.Console.EditorWarning($"Попытка добавить компонент типа {type.Name}, который уже существует");
            return _components.First(c => c.GetType() == type);
        }

        var instance = (Component?)Activator.CreateInstance(type);
        if (instance == null)
        {
            NetEngine.Console.EditorError($"Не удалось создать компонент типа {type.Name}");
            throw new InvalidOperationException($"Cannot create instance of type {type.Name}");
        }

        instance.GameObject = this;
        _components.Add(instance);
        NetEngine.Console.EditorLog($"Компонент {type.Name} добавлен");
        return instance;
    }

    public bool RemoveComponent<T>() where T : Component
    {
        var component = GetComponent<T>();

        if (component == null)
        {
            NetEngine.Console.EditorWarning($"Попытка удалить несуществующий компонент типа {typeof(T).Name}");
            return false;
        }

        if (component is Transform)
        {
            NetEngine.Console.EditorWarning("Попытка удалить Transform, операция отменена");
            return false;
        }

        bool removed = _components.Remove(component);
        NetEngine.Console.EditorLog($"Компонент {typeof(T).Name} удалён: {removed}");
        return removed;
    }

    public bool RemoveComponent(Type type)
    {
        if (type == typeof(Transform))
        {
            NetEngine.Console.EditorWarning("Попытка удалить Transform, операция отменена");
            return false;
        }

        var component = _components.FirstOrDefault(c => c.GetType() == type);

        if (component == null)
        {
            NetEngine.Console.EditorWarning($"Попытка удалить несуществующий компонент типа {type.Name}");
            return false;
        }

        bool removed = _components.Remove(component);
        NetEngine.Console.EditorLog($"Компонент {type.Name} удалён: {removed}");
        return removed;
    }

    public T? GetComponent<T>() where T : Component
    {
        return _components.OfType<T>().FirstOrDefault();
    }

    public List<Component> GetComponents()
    {
        return _components;
    }

    public void SetActive(bool value)
    {
        IsActive = value;
        NetEngine.Console.EditorLog($"GameObject {(IsActive ? "активирован" : "деактивирован")}");
    }

    public void ReloadComponents(Assembly newAssembly)
    {
        NetEngine.Console.EditorLog("[GameObject] Начата перезагрузка компонентов");

        for (int i = 0; i < _components.Count; i++)
        {
            var oldComponent = _components[i];
            if (oldComponent is Transform)
                continue; // Transform не пересоздаём

            var oldType = oldComponent.GetType();
            var newType = newAssembly.GetType(oldType.FullName ?? "");
            if (newType == null || !typeof(Component).IsAssignableFrom(newType))
            {
                NetEngine.Console.EditorWarning($"[GameObject] Новый тип для {oldType.FullName} не найден или не является Component");
                continue;
            }

            var newComponent = (Component?)Activator.CreateInstance(newType);
            if (newComponent == null)
            {
                NetEngine.Console.EditorError($"[GameObject] Не удалось создать экземпляр компонента нового типа {newType.FullName}");
                continue;
            }

            newComponent.GameObject = this;

            CopyFields(oldComponent, newComponent);
            CopyProperties(oldComponent, newComponent);

            _components[i] = newComponent;
            NetEngine.Console.EditorLog($"[GameObject] Компонент {oldType.Name} успешно перезагружен как {newType.Name}");
        }
    }

    private void CopyFields(Component oldComp, Component newComp)
    {
        var oldFields = oldComp.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var newFields = newComp.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var oldField in oldFields)
        {
            var newField = newFields.FirstOrDefault(f => f.Name == oldField.Name && f.FieldType == oldField.FieldType);
            if (newField != null)
            {
                var value = oldField.GetValue(oldComp);
                newField.SetValue(newComp, value);
            }
        }
    }

    private void CopyProperties(Component oldComp, Component newComp)
    {
        var oldProps = oldComp.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(p => p.CanRead && p.CanWrite);
        var newProps = newComp.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(p => p.CanRead && p.CanWrite);

        foreach (var oldProp in oldProps)
        {
            var newProp = newProps.FirstOrDefault(p => p.Name == oldProp.Name && p.PropertyType == oldProp.PropertyType);
            if (newProp != null)
            {
                try
                {
                    var value = oldProp.GetValue(oldComp);
                    newProp.SetValue(newComp, value);
                }
                catch
                {
                    // Игнорируем исключения от несовместимых типов/доступа
                }
            }
        }
    }
}
