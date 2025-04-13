using System;

public interface ICVRPEvaluator {
    public void Initialize(Individual ind);
    public int GetChromLength();
    float Evaluate(Individual ind);

    float LocalOpt(Individual ind);

    void Decode(Individual ind);
}


[Serializable]
public enum CVRPRepresentationType {
    CustomerList,
    CustomerHeuristics,
    RouteCityHeuristics,
}


[Serializable]
public class CVRP2 : IEvaluator{

    public CVRPData cvrpData;
    public float cMax = 1000000;
    public float overCapacityPenalty = 10;

    public IEvaluator evaluator;

    public CVRP2(CVRPData data) {
        this.cvrpData = data;
        switch(cvrpData.representationType) {
            case CVRPRepresentationType.CustomerList:
                evaluator = new CVRPCustomerList(data);
                break;
            case CVRPRepresentationType.CustomerHeuristics:
                evaluator = new CVRPCityHeuristics(data);
                break;
            case CVRPRepresentationType.RouteCityHeuristics:
                evaluator = new CVRPRouteCustomerHeuristics(data);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

    }

    public void Initialize(Individual ind) {
        evaluator.Initialize(ind);
    }

    public float Evaluate(Individual ind) {
        return evaluator.Evaluate(ind);

    }

    public float LocalOpt(Individual ind) {
        return evaluator.LocalOpt(ind);
    }

    public void Decode(Individual ind) {
        evaluator.Decode(ind);
    }

    public int GetChromLength() {
        InputHandler.inst.ThreadLog("GetChromLength: " + evaluator.GetChromLength());
        return evaluator.GetChromLength();
    }

}

 