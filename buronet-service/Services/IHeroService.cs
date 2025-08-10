using buronet_service.Models;
namespace buronet_service.Services
{
    public interface IHeroService
    {
        List<Hero> GetAllHeros(bool? isActive);

        Hero? GetHerosByID(int id);

        Hero AddHero(AddUpdateHero obj);

        Hero? UpdateHero(int id, AddUpdateHero obj);

        bool DeleteHerosByID(int id);
    }
}
