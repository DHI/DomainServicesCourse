import { Theme, makeStyles } from "@material-ui/core";
import { Login as LoginControl, User } from "@dhi/react-components";
import { useStore } from "../index";

interface Token {
  accessToken: {
    /** Access token when successfully login */
    token: string;
    /** Token expiration when successfully login */
    expiration: string;
  };
  refreshToken: {
    /** Refresh token when access token expired */
    token: string;
  };
}

const styles = makeStyles((theme: Theme) => ({
  // present all rows on this page in the center of the column
  container: {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    height: "100%",
  },
  // present all items in the center of the row
  rowContainer: {
    display: "flex",
    flexDirection: "row",
    alignItems: "center",
    flex: 1,
  },
}));

const Login = () => {
  const appStore = useStore();
  const classes = styles();

  const handleLogin = (user: User, token: Token) => {
    appStore.setupSession(
      user.id,
      token.accessToken.token,
      token.refreshToken.token
    );
  };

  return (
    <div className={classes.container}>
      <div className={classes.rowContainer}>
        <LoginControl
          host={process.env.REACT_APP_AUTH_ENDPOINT_URL as string}
          translations={{
            loginButton: "Login",
            userNamePlaceholder: "Username",
            passwordPlaceholder: "Password",
            rememberMeLabel: "Remember me?",
            resetPasswordLabel: "Forgot Password",
            resetPasswordButton: "Forgot Password",
            updatePasswordEmailPlaceholder: "Email Address",
            updatePasswordNewPasswordPlaceholder: "New Password",
            updatePasswordConfirmPasswordPlaceholder: "Confirm Password",
          }}
          showRememberMe={false}
          showResetPassword={false}
          onSuccess={handleLogin}
          textFieldVariant="outlined"
        />
      </div>
    </div>
  );
};

export default Login;
