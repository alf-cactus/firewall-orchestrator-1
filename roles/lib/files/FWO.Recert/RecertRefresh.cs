﻿using System.Diagnostics;
using FWO.Api.Data;
using FWO.Api.Client;
using FWO.Api.Client.Queries;
using FWO.Logging;

namespace FWO.Recert
{
    public class RecertRefresh(ApiConnection apiConnectionIn)
    {
        private readonly ApiConnection apiConnection = apiConnectionIn;

        public async Task<bool> RecalcRecerts()
        {
            Stopwatch watch = new ();

            try
            {
                watch.Start();
                List<FwoOwner> owners = await apiConnection.SendQueryAsync<List<FwoOwner>>(OwnerQueries.getOwners);
                List<Management> managements = await apiConnection.SendQueryAsync<List<Management>>(DeviceQueries.getManagementDetailsWithoutSecrets);
                ReturnId[]? returnIds = (await apiConnection.SendQueryAsync<NewReturning>(RecertQueries.clearOpenRecerts)).ReturnIds;
                Log.WriteDebug("Delete open recerts", $"deleted Ids: {(returnIds != null ? string.Join(",", Array.ConvertAll(returnIds, Id => Id.DeletedId)) : "")}");
                OwnerRefresh? refreshResult = (await apiConnection.SendQueryAsync<List<OwnerRefresh>>(RecertQueries.refreshViewRuleWithOwner)).FirstOrDefault();
                if (refreshResult == null || refreshResult.GetStatus() != "Materialized view refreshed successfully")
                {
                    Log.WriteError("Refresh materialized view view_rule_with_owner", "refresh failed");
                    return true;
                }
                watch.Stop();
                Log.WriteDebug("Refresh materialized view view_rule_with_owner", $"refresh took {(watch.ElapsedMilliseconds / 1000.0).ToString("0.00")} seconds");

                foreach (FwoOwner owner in owners)
                    await RecalcRecertsOfOwner(owner, managements);
            }
            catch (Exception)
            {
                return true;
            }
            return false;
        }

        private async Task RecalcRecertsOfOwner(FwoOwner owner, List<Management> managements)
        {
            Stopwatch watch = new ();
            watch.Start();
            
            foreach (Management mgm in managements)
            {
                List<RecertificationBase> currentRecerts =
                    await apiConnection.SendQueryAsync<List<RecertificationBase>>(RecertQueries.getOpenRecerts, new { ownerId = owner.Id, mgmId = mgm.Id });

                if (currentRecerts.Count > 0)
                {
                    await apiConnection.SendQueryAsync<NewReturning>(RecertQueries.addRecertEntries, new { recerts = currentRecerts });
                }
            }

            watch.Stop();
            Log.WriteDebug("Refresh Recertification", $"refresh for owner {owner.Name} took {(watch.ElapsedMilliseconds / 1000.0).ToString("0.00")} seconds");
        }
    }
}

