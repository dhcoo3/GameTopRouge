using GameFramework;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Skill
{
    public class SkillObj:IReference
    {
        ///<summary>
        ///技能的模板，创建于skillModel，但运行中还是会允许改变
        ///</summary>
        public SkillModel Model;

        ///<summary>
        ///技能等级
        ///</summary>
        public int Level;

        ///<summary>
        ///冷却时间，单位秒。尽管游戏设计里面是没有冷却时间的，但是我们依然需要这个数据
        ///因为作为一个ARPG子分类，和ARPG游戏有一样的问题：一次按键（时间够久）会发生连续多次使用技能，所以得有一个GCD来避免问题
        ///当然和wow的gcd不同，这个“GCD”就只会让当前使用的技能进入0.1秒的冷却
        ///</summary>
        public fix Cooldown;

        public SkillObj(SkillModel model, int level = 1){
            this.Model = model;
            this.Level = level;
            this.Cooldown = 0;
        }

        public void Clear()
        {
            this.Model = null;
            this.Level = 0;
            this.Cooldown = 0;
        }
    }
}