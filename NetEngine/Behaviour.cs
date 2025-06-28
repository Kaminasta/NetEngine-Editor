namespace NetEngine;

public class Behaviour : Component
{
    public bool Enabled = true;

    public bool HasStarted = false;

    public virtual void Start() { }
    public virtual void Update() { }
}

