using System.Collections;
using System.Collections.Generic;
using AAAGame.ScriptsHotfix.GamePlay.Logic.Bullet;
using UnityEngine;

///<summary>
///角色的数值属性部分，比如最大hp、攻击力等等都在这里
///这个建一个结构是因为并非只有角色有这些属性，包括装备、buff、aoe、damageInfo等都会用上
///</summary>
public struct ChaProperty{
    ///<summary>
    ///最大生命，基本都得有，哪怕角色只有1，装备可以是0
    ///</summary>
    public int Hp;

    ///<summary>
    ///攻击力
    ///</summary>
    public int Attack;

    ///<summary>
    ///移动速度，他不是米/秒作为单位的，而是一个可以培养的数值。
    ///具体转化为米/秒，是需要一个规则的，所以是策划脚本 int SpeedToMoveSpeed(int speed)来返回
    ///</summary>
    public int MoveSpeed;

    ///<summary>
    ///行动速度，和移动速度不同，他是增加角色行动速度，也就是变化timeline和动画播放的scale的，比如wow里面开嗜血就是加行动速度
    ///具体多少也不是一个0.2f（我这个游戏中规则设定的最快为正常速度的20%，你的游戏你自己定）到5.0f（我这个游戏设定了最慢是正常速度20%），和移动速度一样需要脚本接口返回策划公式
    ///</summary>
    public int ActionSpeed;

    ///<summary>
    ///弹仓，其实相当于mp了，只是我是射击游戏所以题材需要换皮。
    ///玩家层面理解，跟普通mp上限的区别是角色这个值上限一般都是0，它来自于装备。
    ///</summary>
    public int Ammo;

    ///<summary>
    ///体型圆形半径，用于移动碰撞的，单位：米
    ///这个属性因人而异，但是其实在玩法中几乎不可能经营它，只有buff可能会改变一下，所以直接用游戏中用的数据就行了，不需要转化了
    ///</summary>
    public fix BodyRadius;

    ///<summary>
    ///挨打圆形半径，同体型圆形，只是用途不同，用在判断子弹是否命中的时候
    ///</summary>
    public fix HitRadius;

    ///<summary>
    ///角色移动类型
    ///</summary>
    public BulletDefine.MoveType MoveType;

    /// <summary>
    /// 索敌范围
    /// </summary>
    public int SearchRange;

    public ChaProperty(
        int moveSpeed, 
        int hp = 0, 
        int ammo = 0,
        int attack = 0,
        int actionSpeed = 1, 
        fix bodyRadius = default, 
        fix hitRadius =default, 
        BulletDefine.MoveType moveType = BulletDefine.MoveType.ground,
        int searchRange = 0
    ){
        this.MoveSpeed = moveSpeed;
        this.Hp = hp;
        this.Ammo = ammo;
        this.Attack = attack;
        this.ActionSpeed = actionSpeed;
        this.BodyRadius = bodyRadius;
        this.HitRadius = hitRadius;
        this.MoveType = moveType;
        this.SearchRange = searchRange;
    }

    
    public static ChaProperty zero = new ChaProperty(0,
        0,
        0,
        0,
        0,
        fix.Zero,fix.Zero,0,0);

    ///<summary>
    ///将所有值清0
    ///<param name="moveType">移动类型设置为</param>
    ///</summary>
    public void Zero(BulletDefine.MoveType moveType = BulletDefine.MoveType.ground){
        this.Hp = 0;
        this.MoveSpeed = 0;
        this.Ammo = 0;
        this.Attack = 0;
        this.ActionSpeed = 0;
        this.BodyRadius = fix.Zero;
        this.HitRadius = fix.Zero;
        this.MoveType = moveType;
    }

    //定义加法和乘法的用法，其实这个应该走脚本函数返回，抛给脚本函数多个ChaProperty，由脚本函数运作他们的运算关系，并返回结果
    public static ChaProperty operator +(ChaProperty a, ChaProperty b){
        return new ChaProperty(
            a.MoveSpeed + b.MoveSpeed,
            a.Hp + b.Hp,
            a.Ammo + b.Ammo,
            a.Attack + b.Attack,
            a.ActionSpeed + b.ActionSpeed,
            a.BodyRadius + b.BodyRadius,
            a.HitRadius + b.HitRadius,
            a.MoveType == BulletDefine.MoveType.fly || b.MoveType == BulletDefine.MoveType.fly ? BulletDefine.MoveType.fly : BulletDefine.MoveType.ground,
            a.SearchRange + b.SearchRange
        );
    }
    
    public static ChaProperty operator *(ChaProperty a, ChaProperty b){
        return new ChaProperty(
            fixMath.roundToInt(a.MoveSpeed * (1.0000f + fixMath.max(b.MoveSpeed, -0.9999f))),
            fixMath.roundToInt(a.Hp * (1.0000f + fixMath.max(b.Hp, -0.9999f))),
            fixMath.roundToInt(a.Ammo * (1.0000f + fixMath.max(b.Ammo, -0.9999f))),
            fixMath.roundToInt(a.Attack * (1.0000f + fixMath.max(b.Attack, -0.9999f))),
            fixMath.roundToInt(a.ActionSpeed * (1.0000f + fixMath.max(b.ActionSpeed, -0.9999f))),
            a.BodyRadius * (1.0000f + fixMath.max(b.BodyRadius, -0.9999f)),
            a.HitRadius * (1.0000f + fixMath.max(b.HitRadius, -0.9999f)),
            a.MoveType == BulletDefine.MoveType.fly || b.MoveType == BulletDefine.MoveType.fly ? BulletDefine.MoveType.fly : BulletDefine.MoveType.ground
        );
    }
    
    public static ChaProperty operator *(ChaProperty a, fix b){
        return new ChaProperty(
            fixMath.roundToInt(a.MoveSpeed * b),
            fixMath.roundToInt(a.Hp * b),
            fixMath.roundToInt(a.Ammo * b),
            fixMath.roundToInt(a.Attack * b),
            fixMath.roundToInt(a.ActionSpeed * b),
            a.BodyRadius * b,
            a.HitRadius * b,
            a.MoveType
        );
    }
}