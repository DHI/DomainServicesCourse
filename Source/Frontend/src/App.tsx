import { Theme, makeStyles } from "@material-ui/core";
import { ThemeProvider } from "@dhi/react-components";
import { observer } from "mobx-react";

import { useStore } from "./index";
import "./App.css";
import Login from "./pages/Login";
import Data from "./pages/Data";
import { AppView } from "./stores/AppStateStore";

const styles = makeStyles((theme: Theme) => ({
  app: {
    height: "100vh",
  },
}));

const App = observer(() => {
  const appStore = useStore();
  const classes = styles();

  return (
    <ThemeProvider>
      <div className={classes.app}>
        {appStore.appView === AppView.Login ? <Login /> : <Data />}
      </div>
    </ThemeProvider>
  );
});

export default App;
