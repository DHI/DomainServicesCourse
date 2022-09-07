import { action, makeObservable, observable, runInAction } from "mobx";

export enum AppView {
  Login,
  Data,
}

export interface PointDetails {
  name: string;
  timeseries: string;
}

interface Session {
  user: string;
  accessToken: string;
  refreshToken: string;
}

export class AppStateStore {
  // *******************
  // TODO: If adding new observables here, add their reset also to resetAppState()
  session: Session | null;
  appView: AppView;
  selectedPoint: PointDetails | null;
  version: string | null;

  constructor() {
    this.session = null;
    this.appView = AppView.Login;
    this.selectedPoint = null;
    this.version = null;

    makeObservable(this, {
      session: observable,
      appView: observable,
      selectedPoint: observable,
      version: observable,

      resetAppState: action.bound,
      setAppView: action.bound,
      setSelectedPoint: action.bound,
      setVersion: action.bound,
    });
  }

  setAppView = (view: AppView) => {
    this.appView = view;
  };

  setSelectedPoint = (point: PointDetails) => {
    this.selectedPoint = point;
  };

  setVersion = (version: string) => {
    this.version = version;
  };

  resetAppState = () => {
    this.session = null;
    this.appView = AppView.Login;
    this.selectedPoint = null;
    this.version = null;
  };

  setupSession = (user: string, accessToken: string, refreshToken: string) => {
    runInAction(() => {
      this.appView = AppView.Data;
      this.session = {
        user,
        accessToken,
        refreshToken,
      };
    });
  };

  logout = () => {
    console.log("handleLogout");
    this.resetAppState();
  };
}
