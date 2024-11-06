namespace CeskyBezBolesti_Server.DTO
{
    public class MixReportsOverviewDTO
    {
        public MixReportsOverviewDTO(OneMixReportOverview[] _reports)
        {
            reports = _reports;
        }

        public OneMixReportOverview[] reports;
    }
}
