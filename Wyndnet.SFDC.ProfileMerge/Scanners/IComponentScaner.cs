namespace Wyndnet.SFDC.ProfileMerge
{
    interface IComponentScaner
    {
        DifferenceStore Scan(DifferenceStore diffStore);
    }
}
