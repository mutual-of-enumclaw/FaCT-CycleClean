using System.Threading.Tasks;

namespace Fact.BatchCleaner
{
    public interface ICycleCleaner
    {
        Task Run();
    }
}
