using Quartz;
using Viber.Bot.NetCore.Models;

namespace Viber_AI_Bot.Api.QuartzJobs;

public class PostEndpointJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        ViberCallbackData update = (ViberCallbackData) context.JobDetail.JobDataMap.Get("update");
        
        
        
        return Task.FromResult(true);
    }
}        