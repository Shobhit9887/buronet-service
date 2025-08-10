using buronet_service.Models;
namespace buronet_service.Services
{
    public class HeroService:IHeroService
    {
        private readonly List<Hero> _HeroesList;
        public HeroService()
        {
            _HeroesList = new List<Hero>()
            {
                new Hero(){
                Id = 1,
                FirstName = "Test",
                LastName = "",
                isActive = true,
                }
            };
        }

        public List<Hero> GetAllHeros(bool? isActive)
        {
            return isActive == null ? _HeroesList : _HeroesList.Where(hero => hero.isActive == isActive).ToList();
        }

        public Hero? GetHerosByID(int id)
        {
            return _HeroesList.FirstOrDefault(hero => hero.Id == id);
        }

        public Hero AddHero(AddUpdateHero obj)
        {
            var addHero = new Hero()
            {
                Id = _HeroesList.Max(hero => hero.Id) + 1,
                FirstName = obj.FirstName,
                LastName = obj.LastName,
                isActive = obj.isActive,
            };

            _HeroesList.Add(addHero);

            return addHero;
        }

        public Hero? UpdateHero(int id, AddUpdateHero obj)
        {
            var HeroIndex = _HeroesList.FindIndex(index => index.Id == id);
            if (HeroIndex > 0)
            {
                var hero = _HeroesList[HeroIndex];

                hero.FirstName = obj.FirstName;
                hero.LastName = obj.LastName;
                hero.isActive = obj.isActive;

                _HeroesList[HeroIndex] = hero;

                return hero;
            }
            else
            {
                return null;
            }
        }
        public bool DeleteHerosByID(int id)
        {
            var HeroIndex = _HeroesList.FindIndex(index => index.Id == id);
            if (HeroIndex >= 0)
            {
                _HeroesList.RemoveAt(HeroIndex);
            }
            return HeroIndex >= 0;
        }
    }
}
