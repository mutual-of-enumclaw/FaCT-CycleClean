# FaCT-CycleCleaner

This is a console app to help stem the tide of churn caused in our non-production environments when we don't "care" for a policy.  In production, we issue a policy, the policy holder then pays their premium and it is applied, and, at the end of term, it is cancelled or it is renewed.  In non-production, we often will open a policy for testing and then move on to the next assignment. 
This app is focused on finding those instances and responding to them.  This first instance looks for abandoned policies that have offered a renewal and that renewal was not worked.  In this instance, we build three XML records.  The first record will delete the offered renewal.  The second adds a comment to the prior module, stating that a renewal of that module is not taken, which is a normal instance.  The third message sets that underlying module to RNT, which is Renewal Not Taken. 

This app runs daily and looks for polcies meeting the select condition.  When any are found, these messages are built then inserted to the Communications Framework for processing.
