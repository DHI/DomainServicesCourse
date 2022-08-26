import { action, makeObservable, observable, runInAction } from "mobx";

export enum AppView {
  Login,
  Data,
}

interface Session {
  user: string;
  accessToken: string;
  refreshToken: string;
}

export class AppStateStore {
  // *******************
  // TODO: If adding new observables here, add their reset also to resetAppState()
  appView: AppView;
  session: Session | null;

  constructor() {
    this.session = null;
    this.appView = AppView.Login;

    makeObservable(this, {
      appView: observable,
      session: observable,

      setAppView: action.bound,
      resetAppState: action.bound,
    });
  }

  setAppView = (view: AppView) => {
    this.appView = view;
  };

  resetAppState = () => {
    this.appView = AppView.Login;
    this.session = null;
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
