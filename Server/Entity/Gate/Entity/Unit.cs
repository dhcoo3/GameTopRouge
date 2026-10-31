using Fantasy.Network;

namespace Fantasy.Gate.Entity;

public class Unit:Fantasy.Entitas.Entity
{
    public int UnitId{get;set;}
    public Session? Session{get;set;}
}