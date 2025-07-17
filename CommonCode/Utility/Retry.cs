namespace BFormDomain.HelperClasses;



public static class Retry
{
    public static bool This(
        Action action,
        int limit = 5, int sleep = 500,
        bool silent = false,
        IEnumerable<Type>? noRetryExceptionTypes = null)
    {
        bool done = false;
        int retries = 0;
        Exception? why = null!;

        do
        {
            try
            {
                action();
                done = true;
            }
            catch (Exception ex)
            {
                retries += 1;

                if (null != noRetryExceptionTypes && noRetryExceptionTypes.Contains(ex.GetType()))
                {
                    retries = limit + 1;
                }
                else
                {
                    Thread.Sleep(retries * sleep);
                }

                why = ex;
            }
        } while (!done && retries < limit);

        if (!done && null != why && !silent)
        {
            throw why;

        }

        return done;

    }
}


